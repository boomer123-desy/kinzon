"""Compile actual BOM query methods/DTO with host doubles; execute against SQLite."""
from pathlib import Path
import re
import subprocess
import tempfile


def method(source, name):
    match = re.search(r"        (?:public|private) (?:async )?[^\n]+\b" + name + r"\(", source)
    start = source.index("{", match.start())
    depth = 1
    pos = start + 1
    while depth:
        depth += (source[pos] == "{") - (source[pos] == "}")
        pos += 1
    return source[match.start():pos]


def main():
    module = Path(__file__).resolve().parents[1]
    fixture = Path(__file__).parent / "fixtures"
    folder = Path(tempfile.mkdtemp(prefix="bom-demand-query-"))
    source = (module / "KingZon.OLCM.Production.WebApi/Controllers/BomManageController.cs").read_text(encoding="utf-8")
    controller = (fixture / "bom_demand_support.cs.txt").read_text(encoding="utf-8")
    methods = "\n".join(method(source, name) for name in ["GetProductionBomOptions", "GetProductionBomDataById", "getBomDetailDto", "getBomChildrenData", "transformFormulaDetailType"])
    controller = controller.replace("// ACTUAL_METHODS", methods)
    (folder / "Support.cs").write_text(controller, encoding="utf-8")
    (folder / "Program.cs").write_text((fixture / "bom_demand_program.cs.txt").read_text(encoding="utf-8"), encoding="utf-8")
    dto = module / "KingZon.OLCM.Production.WebApi/Dto/BomDto.cs"
    (folder / "Checks.csproj").write_text(f'''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><FrameworkReference Include="Microsoft.AspNetCore.App"/><PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.26"/><Compile Include="{dto}" Link="BomDto.cs"/></ItemGroup></Project>''', encoding="utf-8")
    print(f"BOM isolated checks: {folder}", flush=True)
    subprocess.run(["dotnet", "run", "--project", str(folder / "Checks.csproj"), "--verbosity", "quiet"], check=True)


if __name__ == "__main__":
    main()
