'''Compile the actual template/query controller with SQLite host doubles, never a business database.'''
from pathlib import Path
import subprocess
import tempfile
workspace = next(p for p in Path(__file__).resolve().parents if (p / 'AGENTS.md').exists())
production = workspace / 'backend/Production'
technical = workspace / 'backend/Technical'
support = (technical / 'tests/production_schedule/fixtures/quantity_support.cs.txt').read_text(encoding='utf-8')
support = support.replace('enum CraftOrderVersionStatus { 待审核', 'enum CraftOrderVersionStatus { 暂存 = 0, 待审核')
support = support.replace('public class Base_Craft : BaseEntity { }', 'public class Base_Craft : BaseEntity { public string Name {get;set;} = "加工"; }')
support = support.replace('  model.Entity<Base_UnitConversion>();', '''  model.Entity<Base_UnitConversion>();
  model.Entity<Base_Department>(); model.Entity<Technical_NodeSettingTemplate_Machine_RL>();
  model.Entity<Base_Machine>().HasMany(x=>x.CraftRLs).WithOne().HasForeignKey(x=>x.MachineId);
  model.Entity<Base_Machine>().HasMany(x=>x.UserRLs).WithOne().HasForeignKey(x=>x.MachineId);''')
fixtures = Path(__file__).parent / 'fixtures'
support += (fixtures / 'schedule_template_support.cs.txt').read_text(encoding='utf-8')
with tempfile.TemporaryDirectory(prefix='schedule-template-') as directory:
    folder = Path(directory)
    (folder / 'Support.cs').write_text(support, encoding='utf-8')
    (folder / 'Program.cs').write_text((fixtures / 'schedule_template_program.cs.txt').read_text(encoding='utf-8'), encoding='utf-8')
    sources = [production / 'KingZon.OLCM.Production.WebApi/Controllers/ProductionSchedueManageController.cs', production / 'KingZon.OLCM.Production.WebApi/Dto/ProductionScheduleDto.cs', technical / 'KingZon.OLCM.Technical.WebApi/Dto/ProductionScheduleQuantityDto.cs']
    sources += [technical / 'KingZon.OLCM.Technical.DataBase/Entities' / name for name in ['Technical_CraftOrder.cs', 'Technical_CraftOrderVersion.cs', 'Technical_NodeSettingTemplate.cs', 'Technical_NodeSettingTemplate_Material_RL.cs']]
    project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><FrameworkReference Include="Microsoft.AspNetCore.App"/><PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.26"/>'
    project += ''.join(f'<Compile Include="{source}" Link="{source.name}"/>' for source in sources)
    project += '</ItemGroup></Project>'
    (folder / 'Checks.csproj').write_text(project, encoding='utf-8')
    subprocess.run(['dotnet', 'run', '--project', str(folder / 'Checks.csproj'), '--verbosity', 'quiet'], check=True)
    cases = (technical / 'tests/production_schedule/fixtures/quantity_cases.cs.txt').read_text(encoding='utf-8')
    cases = cases.replace('=> new(new(db), new(db), new(db), new(db), new(db), new(db), new(db), new(db));',
        '=> new(new(db), new(db), new(db), new(db), new(db), new(db), new(db), new(db), new(db), new(db), new(db), new(db));')
    (folder / 'Program.cs').write_text(cases, encoding='utf-8')
    subprocess.run(['dotnet', 'run', '--project', str(folder / 'Checks.csproj'), '--verbosity', 'quiet'], check=True)


# Keep the existing 40 quantity-query assertions against this same controller and its new constructor.
