"""Compile actual planning entities/maps against isolated EF SQLite host doubles.

No host database connection or migration. Requires .NET SDK 9.
"""
from pathlib import Path
import subprocess
import tempfile


def main():
    module = Path(__file__).resolve().parents[1]
    fixtures = Path(__file__).parent / "fixtures"
    with tempfile.TemporaryDirectory(prefix="production-planning-") as directory:
        folder = Path(directory)
        for name in ["support", "program"]:
            (folder / f"{name}.cs").write_text(
                (fixtures / f"planning_{name}.cs.txt").read_text(encoding="utf-8"), encoding="utf-8")
        names = ["Production_Plan", "Production_Plan_Detail", "Production_Plan_Demand", "Production_Plan_Material_RL",
                 "Production_Dispatch_PlanRL", "Production_NodeSetting", "Production_NodeSetting_RL",
                 "Production_NodeSetting_MachineRL", "Production_NodeSetting_UserRL",
                 "Production_Dispatch_OperationRecord", "Production_Material_Receive_Demand",
                 "Production_Machine_Material_Receive_Demand"]
        sources = [module / "KingZon.OLCM.Production.DataBase/Entities" / f"{name}.cs" for name in names]
        sources.append(module / "KingZon.OLCM.Production.DataBase/Enum/ProductionPlanningStatus.cs")
        project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.26"/>'
        project += "".join(f'<Compile Include="{source}" Link="Models/{source.name}"/>' for source in sources)
        project += "</ItemGroup></Project>"
        (folder / "Checks.csproj").write_text(project, encoding="utf-8")
        subprocess.run(["dotnet", "run", "--project", str(folder / "Checks.csproj"), "--verbosity", "quiet"], check=True)


if __name__ == "__main__":
    main()
