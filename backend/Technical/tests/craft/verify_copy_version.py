"""验证实际复制接口、工序保存及 BOM 同步；宿主公共依赖使用 SQLite 测试替身。

运行：python backend/Technical/tests/craft/verify_copy_version.py
SQLite 不参与 TransactionScope，失败用例另开显式事务验证全部写入可回滚。
"""

from pathlib import Path
import re
import subprocess
import tempfile

from verify_mbom_sync import class_source


def method_source(source, name):
    match = re.search(r"(?:public|private) (?:static )?(?:async )?[^\n]+ " + name + r"\(", source)
    if match is None:
        raise ValueError(f"Missing controller method: {name}")
    opening = source.index("{", match.start())
    depth, cursor = 1, opening + 1
    while depth:
        depth += (source[cursor] == "{") - (source[cursor] == "}")
        cursor += 1
    return source[match.start():cursor]


def main():
    workspace = next(p for p in Path(__file__).resolve().parents if (p / "AGENTS.md").is_file())
    technical = workspace / "backend/Technical"
    controller = technical / "KingZon.OLCM.Technical.WebApi/Controllers/CraftOrderManageController.cs"
    source = controller.read_text(encoding="utf-8-sig")
    fixtures = Path(__file__).resolve().parent / "fixtures"
    folder = Path(tempfile.mkdtemp(prefix="craft-copy-checks-"))
    imports = """using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Globalization;
using System.Transactions;
using KingZon.Core;
using KingZon.Core.DataBase;
using Kingzon.OLCM.Base.Services;
using KingZon.OLCM.Base.Service.Dto;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using KingZon.OLCM.Base.Shared.DataBase.Enum;
using KingZon.OLCM.Base.Shared.DataBase.JsonStructure;
using KingZon.OLCM.Technical.Shared.DataBase.Entities;
using KingZon.OLCM.Technical.Shared.DataBase.Enum;
using KingZon.OLCM.Technical.WebApi.Dto;
using KingZon.OLCM.Production.Shared.DataBase.Entities;
using KingZon.OLCM.Production.Shared.DataBase.Enum;
using KingZon.Mes.Workflow.Model;
"""
    wrapper = imports + """public class CopyController : ControllerBase {
    readonly CommonService<Technical_CraftOrder> _craftOrderService;
    readonly CommonService<Technical_CraftOrderVersion> _craftVersionService;
    readonly CommonService<Technical_NodeSettingTemplate> _nodeSettingService;
    readonly CommonService<Technical_NodeSettingTemplate_Machine_RL> _machineRLService;
    readonly CommonService<Technical_NodeSettingTemplate_Material_RL> _materialRLService;
    readonly CommonService<Base_Craft> _craftService;
    readonly CommonService<Base_Machine> _machineService;
    readonly CommonService<Production_MBom> _mBomService;
    readonly CommonService<Production_MBom_Version> _mBomVersionService;
    readonly CommonService<Production_MBom_Detail> _mBomDetailService;
    readonly IUnitOfWork _unitOfWork;
    readonly Utils _utils;
    readonly ProductService _productService;
    readonly WorkflowService _workflowTemplateService;
    readonly NodeService _nodeTemplateService;
    void DeleteNodeFiles(IEnumerable<long> ids) { throw new Exception("Copy must not delete files"); }
    public CopyController(CheckDb db, Utils utils) {
        _craftOrderService=new(db); _craftVersionService=new(db); _nodeSettingService=new(db);
        _materialRLService=new(db); _machineRLService=new(db); _craftService=new(db); _machineService=new(db);
        _mBomService=new(db); _mBomVersionService=new(db); _mBomDetailService=new(db);
        _unitOfWork=new Work(db); _utils=utils; _productService=new(utils);
        _workflowTemplateService=new(db); _nodeTemplateService=new(db);
    }
    public async Task Sync(Technical_CraftOrderVersion version) { await SyncMBomAsync(version); await _unitOfWork.SaveChangeAsync(); }
"""
    methods = ["CreateCraftVersion", "CopyCraftVersion", "CreateCraftVersionAsync", "GetCopyNodeSettingsAsync",
               "CopyNodeMaterial", "SaveNodeSettingsAsync", "ValidateNodeMachinesAsync", "CreateProcessNodeTemplate",
               "SoftDeleteNodeMachinesAsync", "ReplaceNodeMachinesAsync", "ReplaceNodeMaterialsAsync", "CreateMaterialEntity"]
    wrapper += "\n".join(method_source(source, name) for name in methods)
    wrapper += source.split("#region 制造BOM同步", 1)[1].split("#endregion", 1)[0] + "\n}"
    (folder / "CopyController.cs").write_text(wrapper, encoding="utf-8")
    dto = (technical / "KingZon.OLCM.Technical.WebApi/Dto/CraftDto.cs").read_text(encoding="utf-8-sig")
    dtos = ["CreateCraftVersionDto", "CopyCraftVersionDto", "NodeSettingTemplateDto", "NodeMachineSettingDto", "NodeMaterialRLDto"]
    (folder / "Dtos.cs").write_text(imports + "namespace KingZon.OLCM.Technical.WebApi.Dto {\n"
                                    + "\n".join(class_source(dto, name) for name in dtos) + "\n}", encoding="utf-8")
    for target, filename in [("Support.cs", "copy_version_support.cs.txt"), ("Program.cs", "copy_version_cases.cs.txt")]:
        (folder / target).write_text((fixtures / filename).read_text(encoding="utf-8"), encoding="utf-8")
    entities = ["Technical_CraftOrder.cs", "Technical_CraftOrderVersion.cs", "Technical_NodeSettingTemplate.cs",
                "Technical_NodeSettingTemplate_Machine_RL.cs", "Technical_NodeSettingTemplate_Material_RL.cs",
                "Production_MBom.cs", "Production_MBom_Version.cs", "Production_MBom_Detail.cs"]
    project = """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><FrameworkReference Include="Microsoft.AspNetCore.App" /><PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.26" />"""
    for filename in entities:
        domain = "Production" if filename.startswith("Production_") else "Technical"
        path = workspace / f"backend/{domain}/KingZon.OLCM.{domain}.DataBase/Entities/{filename}"
        project += f'<Compile Include="{path}" Link="Models/{filename}" />'
    for rel in ["backend/Base/KingZon.OLCM.Base.Services/Dto/ProductIdFieldDto.cs",
                "backend/Technical/KingZon.OLCM.Technical.DataBase/Enum/CraftTypes.cs"]:
        project += f'<Compile Include="{workspace / rel}" Link="Models/{Path(rel).name}" />'
    project += """<Reference Include="Microsoft.CodeAnalysis"><HintPath>$(MSBuildToolsPath)/Roslyn/bincore/Microsoft.CodeAnalysis.dll</HintPath></Reference><Reference Include="Microsoft.CodeAnalysis.CSharp"><HintPath>$(MSBuildToolsPath)/Roslyn/bincore/Microsoft.CodeAnalysis.CSharp.dll</HintPath></Reference></ItemGroup></Project>"""
    (folder / "Checks.csproj").write_text(project, encoding="utf-8")
    print(f"Isolated test directory: {folder}", flush=True)
    subprocess.run(["dotnet", "run", "--project", str(folder / "Checks.csproj"), "--verbosity", "quiet", "--", str(controller)], check=True)


if __name__ == "__main__":
    main()
