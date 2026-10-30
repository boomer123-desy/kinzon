"""Compile actual controller/DTOs and verify craft-material queries using SQLite.

Run: python backend/technical/production_schedule/tests/verify_quantity_query.py
Requires Python and .NET SDK 9. Host services/inventory/base types use documented test doubles.
"""
from pathlib import Path
import subprocess
import tempfile


def main():
    module = Path(__file__).resolve().parents[1]
    craft = module.parent / "craft"
    fixtures = Path(__file__).resolve().parent / "fixtures"
    folder = Path(tempfile.mkdtemp(prefix="schedule-quantity-checks-"))
    for target, source in [("Support.cs", "quantity_support.cs.txt"), ("Program.cs", "quantity_cases.cs.txt")]:
        (folder / target).write_text((fixtures / source).read_text(encoding="utf-8"), encoding="utf-8")
    project = """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><FrameworkReference Include="Microsoft.AspNetCore.App"/><PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.26"/>"""
    sources = [*module.glob("controller/*.cs"), *module.glob("dto/*.cs")]
    sources += [craft / "entities" / name for name in ["Technical_CraftOrder.cs", "Technical_CraftOrderVersion.cs", "Technical_NodeSettingTemplate.cs", "Technical_NodeSettingTemplate_Material_RL.cs"]]
    for source in sources:
        project += f'<Compile Include="{source}" Link="Models/{source.name}"/>'
    project += "</ItemGroup></Project>"
    (folder / "Checks.csproj").write_text(project, encoding="utf-8")
    print(f"Isolated test directory: {folder}", flush=True)
    subprocess.run(["dotnet", "run", "--project", str(folder / "Checks.csproj"), "--verbosity", "quiet"], check=True)


if __name__ == "__main__":
    main()
