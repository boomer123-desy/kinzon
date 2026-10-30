"""用实际 BOM 实体及控制器同步方法进行隔离 SQLite 回归；需要 Python 和 .NET SDK 9。

运行：python backend/technical/craft/tests/verify_mbom_sync.py
公共服务、基础资料和工作流类型使用测试替身，不能替代完整宿主项目联调。
"""

from pathlib import Path
import subprocess
import tempfile


def class_source(source, name):
    start = source.index(f"public class {name}")
    opening = source.index("{", start)
    depth = 1
    cursor = opening + 1
    while depth:
        depth += (source[cursor] == "{") - (source[cursor] == "}")
        cursor += 1
    return source[start:cursor]


def main():
    root = Path(__file__).resolve().parents[1]
    fixtures = Path(__file__).resolve().parent / "fixtures"
    folder = Path(tempfile.mkdtemp(prefix="craft-mbom-checks-"))
    controller = root / "controller" / "CraftOrderManageController.cs"
    source = controller.read_text(encoding="utf-8-sig")
    region = source.split("#region 制造BOM同步", 1)[1].split("#endregion", 1)[0]
    imports = """using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Globalization;
using KingZon.Core;
using KingZon.Core.DataBase;
using Kingzon.OLCM.Base.Services;
using KingZon.OLCM.Base.Service.Dto;
using KingZon.OLCM.Base.Shared.DataBase.Enum;
using KingZon.OLCM.Base.Shared.DataBase.JsonStructure;
using KingZon.OLCM.Production.Shared.DataBase.Entities;
using KingZon.OLCM.Production.Shared.DataBase.Enum;
using KingZon.OLCM.Technical.Shared.DataBase.Entities;
using KingZon.OLCM.Technical.Shared.DataBase.Enum;
"""
    wrapper = imports + """public class SyncController {
    readonly CommonService<Technical_CraftOrder> _craftOrderService;
    readonly CommonService<Technical_CraftOrderVersion> _craftVersionService;
    readonly CommonService<Technical_NodeSettingTemplate> _nodeSettingService;
    readonly CommonService<Technical_NodeSettingTemplate_Material_RL> _materialRLService;
    readonly CommonService<Production_MBom> _mBomService;
    readonly CommonService<Production_MBom_Version> _mBomVersionService;
    readonly CommonService<Production_MBom_Detail> _mBomDetailService;
    readonly IUnitOfWork _unitOfWork;
    readonly Utils _utils;
    readonly ProductService _productService;
    public SyncController(CheckDb db, Utils utils) {
        _craftOrderService=new(db); _craftVersionService=new(db); _nodeSettingService=new(db); _materialRLService=new(db);
        _mBomService=new(db); _mBomVersionService=new(db); _mBomDetailService=new(db);
        _unitOfWork=new Work(db); _utils=utils; _productService=new(utils);
    }
    public async Task Sync(Technical_CraftOrderVersion version) { await SyncMBomAsync(version); await _unitOfWork.SaveChangeAsync(); }
    public async Task Metadata(Technical_CraftOrderVersion version) { await SyncMBomMetadataAsync(version); await _unitOfWork.SaveChangeAsync(); }
    public async Task Archive(Technical_CraftOrderVersion version) { await ArchiveMBomVersionAsync(version); await _unitOfWork.SaveChangeAsync(); }
""" + region + "\n}\n"
    (folder / "SyncController.cs").write_text(wrapper, encoding="utf-8")
    for filename, fixture in [("Support.cs", "mbom_support.cs.txt"), ("Program.cs", "mbom_cases.cs.txt")]:
        (folder / filename).write_text((fixtures / fixture).read_text(encoding="utf-8"), encoding="utf-8")

    dto = (root / "dto" / "CraftDto.cs").read_text(encoding="utf-8-sig")
    output_dtos = imports + "namespace KingZon.OLCM.Technical.WebApi.Dto {\n"
    output_dtos += class_source(dto, "CraftVersionDto") + "\n" + class_source(dto, "CraftVersionDetailDto")
    output_dtos += "\npublic class NodeSettingTemplateDto {}\n}"
    (folder / "ResponseDtos.cs").write_text(output_dtos, encoding="utf-8")
    entities = ["Production_MBom.cs", "Production_MBom_Version.cs", "Production_MBom_Detail.cs",
                "Technical_CraftOrder.cs", "Technical_CraftOrderVersion.cs", "Technical_NodeSettingTemplate_Material_RL.cs"]
    project = """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.26" />"""
    for filename in entities:
        project += f'<Compile Include="{root / "entities" / filename}" Link="Models/{filename}" />'
    project += f'<Compile Include="{root / "dto" / "ProductIdFieldDto.cs"}" Link="Models/ProductIdFieldDto.cs" />'
    project += """<Reference Include="Microsoft.CodeAnalysis"><HintPath>$(MSBuildToolsPath)/Roslyn/bincore/Microsoft.CodeAnalysis.dll</HintPath></Reference><Reference Include="Microsoft.CodeAnalysis.CSharp"><HintPath>$(MSBuildToolsPath)/Roslyn/bincore/Microsoft.CodeAnalysis.CSharp.dll</HintPath></Reference></ItemGroup></Project>"""
    (folder / "Checks.csproj").write_text(project, encoding="utf-8")
    print(f"Isolated test directory: {folder}", flush=True)
    subprocess.run(["dotnet", "run", "--project", str(folder / "Checks.csproj"), "--verbosity", "quiet", "--", str(controller)], check=True)


if __name__ == "__main__":
    main()
