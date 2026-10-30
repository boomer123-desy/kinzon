"""Compile the actual report controller/DTO and run its queries against SQLite.

Missing host entity/service definitions are represented by test doubles whose
fields follow the existing legacy controller call sites. This is not a host build.
Run: python backend/sale_return_order/tests/verify_month_sale_stat.py
"""
from pathlib import Path
import subprocess
import tempfile


def main():
    module = Path(__file__).resolve().parents[1]
    folder = Path(tempfile.mkdtemp(prefix="month-sale-stat-"))
    fixtures = Path(__file__).resolve().parent / "fixtures"
    for name in ["Support", "Program"]:
        (folder / f"{name}.cs").write_text((fixtures / f"month_sale_{name.lower()}.cs.txt").read_text(encoding="utf-8"), encoding="utf-8")
    project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><FrameworkReference Include="Microsoft.AspNetCore.App"/><PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.26"/>'
    for source in [module / "controller/SaleDataStatController.cs", module / "controller/dto/SaleDataStatDTO.cs"]:
        project += f'<Compile Include="{source}" Link="{source.name}"/>'
    template = module / "ExcelTemplates/MonthSaleDataStat.xlsx"
    project += f'<None Include="{template}" Link="ExcelTemplates/MonthSaleDataStat.xlsx" CopyToOutputDirectory="PreserveNewest"/>'
    project += '<PackageReference Include="Aspose.Cells" Version="26.9.0"/></ItemGroup></Project>'
    (folder / "Checks.csproj").write_text(project, encoding="utf-8")
    print(f"Isolated report checks: {folder}", flush=True)
    subprocess.run(["dotnet", "run", "--project", str(folder / "Checks.csproj"), "--verbosity", "quiet"], check=True)


if __name__ == "__main__":
    main()
