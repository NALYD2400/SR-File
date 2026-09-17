import os, shutil

release_dir = r"Release"
os.makedirs(release_dir, exist_ok=True)

# Copy from CodeWalker\bin\Release\net48
src_cw = r"CodeWalker\bin\Release\net48"
for f in os.listdir(src_cw):
    src_f = os.path.join(src_cw, f)
    if os.path.isfile(src_f):
        dst_f = os.path.join(release_dir, f)
        shutil.copy2(src_f, dst_f)
        print(f"Copied: {f}")

# Copy from other subprojects
subprojects = [
    r"CodeWalker.RPFExplorer\bin\Release\net48",
    r"CodeWalker.Peds\bin\Release\net48",
    r"CodeWalker.Vehicles\bin\Release\net48",
    r"CodeWalker.ModManager\bin\Release\net48",
    r"CodeWalker.Gen9Converter\bin\Release\net48",
    r"CodeWalker.ErrorReport\bin\Release\net48"
]

for sub in subprojects:
    if os.path.exists(sub):
        for f in os.listdir(sub):
            if f.endswith(('.exe', '.config', '.pdb')):
                src_f = os.path.join(sub, f)
                dst_f = os.path.join(release_dir, f)
                shutil.copy2(src_f, dst_f)
                print(f"Copied: {f}")

# Copy logo and ico
shutil.copy2(r"sr_logo.png", os.path.join(release_dir, "sr_logo.png"))
shutil.copy2(r"sr_app_icon.ico", os.path.join(release_dir, "sr_app_icon.ico"))
shutil.copy2(r"sr_app_icon.ico", os.path.join(release_dir, "CW.ico"))

print("\nDeployment to Release complete!")





