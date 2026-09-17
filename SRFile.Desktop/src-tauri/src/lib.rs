#[cfg(windows)]
use std::os::windows::process::CommandExt;
use std::process::{Child, Command, Stdio};
use std::sync::{Arc, Mutex};
use std::time::Duration;
use tauri::State;

const DEFAULT_SIDECAR_PORT: u16 = 5890;

pub struct SidecarState {
    pub child: Arc<Mutex<Option<Child>>>,
    pub port: u16,
}

fn check_sidecar_health(port: u16) -> bool {
    let url = format!("http://127.0.0.1:{}/api/status", port);
    let client = reqwest::blocking::Client::builder()
        .timeout(Duration::from_millis(800))
        .build();

    if let Ok(c) = client {
        if let Ok(res) = c.get(&url).send() {
            return res.status().is_success();
        }
    }
    false
}

fn find_sidecar_path() -> Option<(String, Vec<String>)> {
    let mut search_dirs = Vec::new();

    if let Ok(cwd) = std::env::current_dir() {
        search_dirs.push(cwd);
    }

    if let Ok(exe) = std::env::current_exe() {
        let mut curr = exe.parent();
        for _ in 0..5 {
            if let Some(p) = curr {
                search_dirs.push(p.to_path_buf());
                curr = p.parent();
            } else {
                break;
            }
        }
    }

    // 1. Check native binary executable candidates
    let exe_rel_paths = [
        "SRFile.Sidecar.exe",
        "SRFile.Sidecar/bin/Release/net8.0/SRFile.Sidecar.exe",
        "SRFile.Sidecar/bin/Debug/net8.0/SRFile.Sidecar.exe",
        "../SRFile.Sidecar/bin/Release/net8.0/SRFile.Sidecar.exe",
        "../SRFile.Sidecar/bin/Debug/net8.0/SRFile.Sidecar.exe",
        "../../SRFile.Sidecar/bin/Release/net8.0/SRFile.Sidecar.exe",
        "../../SRFile.Sidecar/bin/Debug/net8.0/SRFile.Sidecar.exe",
        "../../../SRFile.Sidecar/bin/Release/net8.0/SRFile.Sidecar.exe",
        "../../../SRFile.Sidecar/bin/Debug/net8.0/SRFile.Sidecar.exe",
        "../../../../SRFile.Sidecar/bin/Release/net8.0/SRFile.Sidecar.exe",
        "../../../../SRFile.Sidecar/bin/Debug/net8.0/SRFile.Sidecar.exe",
    ];

    for base in &search_dirs {
        for rel in &exe_rel_paths {
            let candidate = base.join(rel);
            if candidate.is_file() {
                return Some((
                    candidate.to_string_lossy().to_string(),
                    vec!["--port".into(), DEFAULT_SIDECAR_PORT.to_string()],
                ));
            }
        }
    }

    // 2. Check DLL path to run via dotnet
    let dll_rel_paths = [
        "SRFile.Sidecar.dll",
        "SRFile.Sidecar/bin/Release/net8.0/SRFile.Sidecar.dll",
        "SRFile.Sidecar/bin/Debug/net8.0/SRFile.Sidecar.dll",
        "../SRFile.Sidecar/bin/Release/net8.0/SRFile.Sidecar.dll",
        "../SRFile.Sidecar/bin/Debug/net8.0/SRFile.Sidecar.dll",
        "../../SRFile.Sidecar/bin/Release/net8.0/SRFile.Sidecar.dll",
        "../../SRFile.Sidecar/bin/Debug/net8.0/SRFile.Sidecar.dll",
        "../../../SRFile.Sidecar/bin/Release/net8.0/SRFile.Sidecar.dll",
        "../../../SRFile.Sidecar/bin/Debug/net8.0/SRFile.Sidecar.dll",
    ];

    for base in &search_dirs {
        for rel in &dll_rel_paths {
            let candidate = base.join(rel);
            if candidate.is_file() {
                return Some((
                    "dotnet".into(),
                    vec![
                        candidate.to_string_lossy().to_string(),
                        "--port".into(),
                        DEFAULT_SIDECAR_PORT.to_string(),
                    ],
                ));
            }
        }
    }

    // 3. Fallback: dotnet run
    let proj_rel_paths = [
        "SRFile.Sidecar/SRFile.Sidecar.csproj",
        "../SRFile.Sidecar/SRFile.Sidecar.csproj",
        "../../SRFile.Sidecar/SRFile.Sidecar.csproj",
        "../../../SRFile.Sidecar/SRFile.Sidecar.csproj",
    ];

    for base in &search_dirs {
        for rel in &proj_rel_paths {
            let candidate = base.join(rel);
            if candidate.is_file() {
                return Some((
                    "dotnet".into(),
                    vec![
                        "run".into(),
                        "--project".into(),
                        candidate.to_string_lossy().to_string(),
                        "--".into(),
                        "--port".into(),
                        DEFAULT_SIDECAR_PORT.to_string(),
                    ],
                ));
            }
        }
    }

    None
}

fn start_sidecar(port: u16) -> Option<Child> {
    if check_sidecar_health(port) {
        println!("[SRFile.Shell] Sidecar already alive on port {}", port);
        return None;
    }

    if let Some((cmd, args)) = find_sidecar_path() {
        println!("[SRFile.Shell] Spawning sidecar: {} {:?}", cmd, args);
        let mut cmd_builder = Command::new(&cmd);
        cmd_builder
            .args(&args)
            .stdout(Stdio::inherit())
            .stderr(Stdio::inherit());

        #[cfg(windows)]
        {
            const CREATE_NO_WINDOW: u32 = 0x08000000;
            cmd_builder.creation_flags(CREATE_NO_WINDOW);
        }

        let child = cmd_builder.spawn();

        match child {
            Ok(c) => {
                // Wait for sidecar to become healthy
                for _ in 0..30 {
                    std::thread::sleep(Duration::from_millis(300));
                    if check_sidecar_health(port) {
                        println!("[SRFile.Shell] Sidecar successfully initialized and responding on port {}", port);
                        return Some(c);
                    }
                }
                println!("[SRFile.Shell] Sidecar launched but not responding to health checks in time.");
                Some(c)
            }
            Err(e) => {
                eprintln!("[SRFile.Shell] Failed to spawn sidecar process: {}", e);
                None
            }
        }
    } else {
        eprintln!("[SRFile.Shell] Could not find SRFile.Sidecar binary or project.");
        None
    }
}

#[tauri::command]
fn get_sidecar_url(state: State<'_, SidecarState>) -> String {
    format!("http://127.0.0.1:{}", state.port)
}

#[tauri::command]
fn get_sidecar_status(state: State<'_, SidecarState>) -> Result<serde_json::Value, String> {
    let url = format!("http://127.0.0.1:{}/api/status", state.port);
    let client = reqwest::blocking::Client::builder()
        .timeout(Duration::from_secs(2))
        .build()
        .map_err(|e| e.to_string())?;

    let res = client.get(&url).send().map_err(|e| e.to_string())?;
    let json: serde_json::Value = res.json().map_err(|e| e.to_string())?;
    Ok(json)
}

#[tauri::command]
fn select_file(title: Option<String>, filter_name: Option<String>, extensions: Option<Vec<String>>) -> Option<String> {
    let mut dialog = rfd::FileDialog::new();
    if let Some(t) = title {
        dialog = dialog.set_title(&t);
    }
    if let (Some(name), Some(exts)) = (filter_name, extensions) {
        let refs: Vec<&str> = exts.iter().map(|s| s.as_str()).collect();
        dialog = dialog.add_filter(&name, &refs);
    }

    dialog.pick_file().map(|p| p.to_string_lossy().to_string())
}

#[tauri::command]
fn select_folder(title: Option<String>) -> Option<String> {
    let mut dialog = rfd::FileDialog::new();
    if let Some(t) = title {
        dialog = dialog.set_title(&t);
    }
    dialog.pick_folder().map(|p| p.to_string_lossy().to_string())
}

#[tauri::command]
fn restart_sidecar(state: State<'_, SidecarState>) -> Result<bool, String> {
    let mut lock = state.child.lock().unwrap();
    if let Some(ref mut child) = *lock {
        let _ = reqwest::blocking::Client::builder()
            .timeout(Duration::from_millis(300))
            .build()
            .map(|c| c.post(format!("http://127.0.0.1:{}/api/shutdown", state.port)).send());
        std::thread::sleep(Duration::from_millis(150));
        let _ = child.kill();
    }
    *lock = None;

    // Wait a brief moment
    std::thread::sleep(Duration::from_millis(300));

    let new_child = start_sidecar(state.port);
    let healthy = check_sidecar_health(state.port);
    *lock = new_child;

    Ok(healthy)
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    let child_handle = start_sidecar(DEFAULT_SIDECAR_PORT);
    let sidecar_state = SidecarState {
        child: Arc::new(Mutex::new(child_handle)),
        port: DEFAULT_SIDECAR_PORT,
    };

    let child_cleanup = sidecar_state.child.clone();

    tauri::Builder::default()
        .plugin(tauri_plugin_opener::init())
        .manage(sidecar_state)
        .invoke_handler(tauri::generate_handler![
            get_sidecar_url,
            get_sidecar_status,
            select_file,
            select_folder,
            restart_sidecar
        ])
        .build(tauri::generate_context!())
        .expect("error while running tauri application")
        .run(move |_app_handle, event| {
            if let tauri::RunEvent::Exit = event {
                println!("[SRFile.Shell] Cleaning up sidecar process before exit...");
                let _ = reqwest::blocking::Client::builder()
                    .timeout(Duration::from_millis(300))
                    .build()
                    .map(|c| c.post(format!("http://127.0.0.1:{}/api/shutdown", DEFAULT_SIDECAR_PORT)).send());
                std::thread::sleep(Duration::from_millis(150));
                let mut lock = child_cleanup.lock().unwrap();
                if let Some(ref mut child) = *lock {
                    let _ = child.kill();
                }
            }
        });
}
