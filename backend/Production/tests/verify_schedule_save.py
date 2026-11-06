"""Execute the actual schedule controller, TPT entities and workflow service in isolated SQLite.
Host repositories/base inventory classes are doubles; no business database is accessed.
SQLite ignores ambient transactions, so this test does not assert MySQL rollback/locking.
"""
from pathlib import Path
import subprocess
import tempfile

root = next(p for p in Path(__file__).resolve().parents if (p / 'AGENTS.md').exists())
production = root / 'backend/Production'
technical = root / 'backend/Technical'
fixtures = Path(__file__).parent / 'fixtures'
with tempfile.TemporaryDirectory(prefix='schedule-save-') as directory:
    folder = Path(directory)
    for name in ['support', 'program']:
        (folder / f'{name}.cs').write_text((fixtures / f'schedule_save_{name}.cs.txt').read_text(encoding='utf-8'), encoding='utf-8')
    sources = [production / 'KingZon.OLCM.Production.WebApi/Controllers/ProductionSchedueManageController.cs',
               production / 'KingZon.OLCM.Production.WebApi/Dto/ProductionScheduleDto.cs',
               technical / 'KingZon.OLCM.Technical.WebApi/Dto/ProductionScheduleQuantityDto.cs',
               production / 'KingZon.OLCM.Production.DataBase/service/WorkflowService.cs']
    for name in ['Technical_CraftOrder', 'Technical_CraftOrderVersion', 'WF_NodeTemplate_ProductionExt', 'WF_NodeTemplate_Material_RL', 'WF_NodeTemplate_Machine_RL']:
        sources.append(technical / f'KingZon.OLCM.Technical.DataBase/Entities/{name}.cs')
    for name in ['WF_Node', 'WF_NodeLink', 'WF_NodeTemplate', 'WF_NodeType', 'WF_Workflow', 'WF_WorkflowType', 'WF_WorkflowTemplate',
                 'WF_Node_ProductionExt', 'WF_Node_ProductionExt_MachineRL', 'WF_Node_ProductionExt_MaterialRL',
                 'Production_Dispatch_Order', 'Production_Material_Receive_Demand', 'Production_Machine_Material_Receive_Demand']:
        sources.append(production / f'KingZon.OLCM.Production.DataBase/Entities/{name}.cs')
    for name in ['NodeType', 'NodeState', 'WorkflowState', 'DispatchOrderStatus', 'DispatchBasisType', 'ProductionScheduleNodeType']:
        sources.append(production / f'KingZon.OLCM.Production.DataBase/Enum/{name}.cs')
    project = '''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><FrameworkReference Include="Microsoft.AspNetCore.App"/><PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.26"/>'''
    project += ''.join(f'<Compile Include="{p}" Link="{p.name}"/>' for p in sources)
    (folder / 'Checks.csproj').write_text(project + '</ItemGroup></Project>', encoding='utf-8')
    subprocess.run(['dotnet', 'run', '--project', str(folder / 'Checks.csproj'), '--verbosity', 'quiet'], check=True)
