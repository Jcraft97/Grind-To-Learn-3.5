modlist = [
    2298956701, # Concrete Mod
]

import xml.etree.ElementTree as ET
import glob
import os
from zipfile import ZipFile

if not os.getcwd().endswith('Data'): os.chdir("Data") # for vscode

default_steamapps = (
    os.path.join(os.environ.get('PROGRAMFILES(X86)', r'C:\Program Files (x86)'), 'Steam', 'steamapps')
    if os.name == 'nt'
    else '/home/cat/.steam/steam/steamapps'
)
steamapps = os.environ.get('STEAMAPPS', default_steamapps)
CONTENT = os.path.join(steamapps, 'common', 'SpaceEngineers', 'Content') + os.sep
MODS = os.path.join(steamapps, 'workshop', 'content', '244850') + os.sep

xml_stuff = {'xmlns:xsd':'http://www.w3.org/2001/XMLSchema',
             'xmlns:xsi':'http://www.w3.org/2001/XMLSchema-instance'}

unlockersCount = 250
keenOverwrite = 50

def autoformat_xml(elem, level=0):
    i = "\n" + level*"\t"
    if len(elem):
        if not elem.text or not elem.text.strip():
            elem.text = i + "\t"
        if not elem.tail or not elem.tail.strip():
            elem.tail = i
        for elem in elem:
            autoformat_xml(elem, level+1)
        if not elem.tail or not elem.tail.strip():
            elem.tail = i
    else:
        if level and (not elem.tail or not elem.tail.strip()):
            elem.tail = i

def save_xml(filename, root):
    print("SAVING XML", filename)
    autoformat_xml(root)
    h = open(filename , "wb")
    etree = ET.ElementTree(root)
    etree.write(h, encoding='utf-8', xml_declaration=True)
    h.close()
    
def load_unlocker_template():
    tree = ET.parse('Unlocker.template')
    root = tree.getroot()
    return root.find('CubeBlocks').find('Definition')

def xml_replace_text(root, a, b):
    for e in root.iter():
        if e.text:
            e.text = e.text.replace(a, b)
        if e.tail:
            e.tail = e.tail.replace(a, b)

def generate_ResearchGroups_sbc():
    Definitions = ET.Element('Definitions', xml_stuff)
    ResearchGroups = ET.SubElement(Definitions, 'ResearchGroups')

    for i in range(keenOverwrite):

        ResearchGroup = ET.SubElement(ResearchGroups, 'ResearchGroup', {'xsi:type':'ResearchGroup'})
        Id = ET.SubElement(ResearchGroup, 'Id', {'Type':'MyObjectBuilder_ResearchGroupDefinition', 'Subtype':str(i) })
        Members = ET.SubElement(ResearchGroup, 'Members')
        if(i == 0):
            BlockId = ET.SubElement(Members, 'BlockId', {'Type':"CubeBlock", 'Subtype':"EngineerPlushie"}) # avoid SE crash on .cs error

    save_xml('ResearchGroups.sbc', Definitions)

def generate_Unlockers_sbc():
    Unlockers = ET.Element('Definitions', xml_stuff)
    CubeBlocks = ET.SubElement(Unlockers, 'CubeBlocks')

    for i in range(unlockersCount):

        Type = 'MyObjectBuilder_ResearchGroupDefinition'
        Subtype = 'U' + str(i)
    
        t = load_unlocker_template()
        xml_replace_text(t, '{0}', Subtype)

        CubeBlocks.append(t)

    save_xml('Unlockers.sbc', Unlockers)

def generate_UnlockersResearchGroups_sbc():
    UnlockersGroups = ET.Element('Definitions', xml_stuff)
    ResearchGroups = ET.SubElement(UnlockersGroups, 'ResearchGroups')

    for i in range(unlockersCount):

        Type = 'MyObjectBuilder_ResearchGroupDefinition'
        Subtype = 'U' + str(i)
    
        ResearchGroup = ET.SubElement(ResearchGroups, 'ResearchGroup', {'xsi:type':'ResearchGroup'})
        Id = ET.SubElement(ResearchGroup, 'Id', {'Type':Type, 'Subtype':Subtype})
        Members = ET.SubElement(ResearchGroup, 'Members')

    save_xml('UnlockersResearchGroups.sbc', UnlockersGroups)

def process_sbc(sbc_text, keyword, root_must_be):
    
    result = set()
    
    #tree = ET.parse(sbc)
    #root = tree.getroot()
    root = ET.fromstring(sbc_text)

    if root_must_be is not None:
        root = root.find(root_must_be)
        if root is None: return result

    ignore_list = ['DebugSphere', 'Monolith', 'Stereolith', 'Plushie'] # hardcoded

    for Definition in root.iter(keyword):
        Id = Definition.find('Id')

        if Id is None: continue
        
        if 'Type' in Id.attrib and 'Subtype' in Id.attrib:
            Type = Id.attrib['Type']
            Subtype = Id.attrib['Subtype']

        else:
            TypeId = Id.find('TypeId')
            SubtypeId = Id.find('SubtypeId')

            if TypeId is None: continue
            if SubtypeId is None: continue

            Type, Subtype = TypeId.text, SubtypeId.text

        if Subtype is None: Subtype = ''

        Type = Type.replace('MyObjectBuilder_', '')

        ignore = False
        for i in ignore_list:
            if i in Type or i in Subtype:
                ignore = True
        if ignore: continue

        result.add((Type, Subtype))

    return result

def get_all_block_types(mod_pathname, keyword, root_must_be = None):

    result = set()

    #ignore_list = []

    sbcs = glob.glob(mod_pathname, recursive = True)
    if len(sbcs) != 0:
        sbcs.sort()

        for sbc in sbcs:
            with open(sbc, 'rb') as f:
                content = f.read()
                s = process_sbc(content, keyword, root_must_be)
                
                if(s is not None): result = result.union(s);
    
    return result

def get_all_block_types_from_zip(zip_pathname, keyword, root_must_be = None):
    
    result = set()

    zf = glob.glob(zip_pathname)

    if len(zf) != 1:
        return result
    zf = zf[0]
    
    print("OLD MOD FORMAT");

    archive = ZipFile(zf, 'r')
    files = archive.namelist()

    for name in archive.namelist():

        if not name.endswith('.sbc'): continue

        content = archive.read(name)
        s = process_sbc(content, keyword, root_must_be)

        if(s is not None): result = result.union(s);

    return result

def save_research_blocks(blocks, output_xml):
    Definitions = ET.Element('Definitions', xml_stuff)
    ResearchBlocks = ET.SubElement(Definitions, 'ResearchBlocks')

    for block in sorted(blocks):
    
        Type, Subtype = block
        
        ResearchBlock = ET.SubElement(ResearchBlocks, 'ResearchBlock', {'xsi:type':'ResearchBlock'})
        
        ET.SubElement(ResearchBlock, 'Id', {'Type':Type, 'Subtype':Subtype})

        UnlockedByGroups = ET.SubElement(ResearchBlock, 'UnlockedByGroups')
        
    save_xml(output_xml, Definitions)

def save_as_text(blocks, out):
    for b in blocks:
        type, subtype = b;
        print(type + '/' + subtype, file=out)

generate_Unlockers_sbc()
generate_UnlockersResearchGroups_sbc()
generate_ResearchGroups_sbc()

#text = open('defs.txt', 'w')

cb = get_all_block_types(CONTENT + 'Data/CubeBlocks/*.sbc', 'Definition', 'CubeBlocks')
rb = get_all_block_types(CONTENT + 'Data/ResearchBlocks.sbc', 'ResearchBlock')
ndrb = cb.difference(rb)

print('CubeBlocks', len(cb))
print('ResearchBlocks', len(rb))
print('Not defined RB', len(ndrb))

save_research_blocks(ndrb, 'ResearchBlocks.sbc')

#save_as_text(cb, text)

cb = rb = ndrb = None

for m in modlist:
    m = str(m)
    print('>>> MOD', m)
    mod_cb = get_all_block_types(MODS+m+'/**/*.sbc', 'Definition', 'CubeBlocks')
    mod_rb = get_all_block_types(MODS+m+'/**/*.sbc', 'ResearchBlock')

    if len(mod_cb) + len(mod_rb) == 0:
        mod_cb = get_all_block_types_from_zip(MODS+m+'/*.bin', 'Definition', 'CubeBlocks')
        mod_rb =  get_all_block_types_from_zip(MODS+m+'/*.bin', 'ResearchBlock')
    
    mod_ndrb = mod_cb.difference(mod_rb)
    
    print('CubeBlocks', len(mod_cb))
    print('ResearchBlocks', len(mod_rb))
    print('Not defined RB', len(mod_ndrb))

    save_research_blocks(mod_ndrb, 'Mods/'+m+'.sbc')

    #save_as_text(mod_cb, text)

print("DONE")
