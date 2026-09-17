import os, shutil

destinations = [r"Release", r"."]
os.makedirs(r"Release", exist_ok=True)

src_cw = r"CodeWalker\bin\Release\net48"
subprojects = [
    r"CodeWalker.RPFExplorer\bin\Release\net48",
    r"CodeWalker.Peds\bin\Release\net48",
    r"CodeWalker.Vehicles\bin\Release\net48",
    r"CodeWalker.ModManager\bin\Release\net48",
    r"CodeWalker.Gen9Converter\bin\Release\net48",
    r"CodeWalker.ErrorReport\bin\Release\net48"
]

for dest in destinations:
    # 1. Copy all outputs from main CodeWalker project
    if os.path.exists(src_cw):
        for f in os.listdir(src_cw):
            src_f = os.path.join(src_cw, f)
            if os.path.isfile(src_f) and not f.startswith("test_") and not f.startswith("DxNavbarTest"):
                dst_f = os.path.join(dest, f)
                if os.path.abspath(src_f) != os.path.abspath(dst_f):
                    try:
                        shutil.copy2(src_f, dst_f)
                        print(f"[{dest}] Copied: {f}")
                    except Exception as e:
                        print(f"[{dest}] Error copying {f}: {e}")

    # 2. Copy specific subproject outputs (only their specific executables and configs)
    for sub in subprojects:
        if os.path.exists(sub):
            for f in os.listdir(sub):
                if f.startswith("SR File ") and not f.startswith("SR File.exe"):
                    src_f = os.path.join(sub, f)
                    dst_f = os.path.join(dest, f)
                    if os.path.abspath(src_f) != os.path.abspath(dst_f):
                        try:
                            shutil.copy2(src_f, dst_f)
                            print(f"[{dest}] Copied subproject: {f}")
                        except Exception as e:
                            print(f"[{dest}] Error copying subproject {f}: {e}")

    # 3. Copy icons/logos if present and not the same destination
    if os.path.exists(r"sr_logo.png") and os.path.abspath(r"sr_logo.png") != os.path.abspath(os.path.join(dest, "sr_logo.png")):
        shutil.copy2(r"sr_logo.png", os.path.join(dest, "sr_logo.png"))
    if os.path.exists(r"sr_app_icon.ico") and os.path.abspath(r"sr_app_icon.ico") != os.path.abspath(os.path.join(dest, "sr_app_icon.ico")):
        shutil.copy2(r"sr_app_icon.ico", os.path.join(dest, "sr_app_icon.ico"))
        shutil.copy2(r"sr_app_icon.ico", os.path.join(dest, "CW.ico"))

print("\nDeployment to Release and project root completed successfully!")