"""Generate an offset/type index, not game content, from a reviewed Unity build.

Requires UnityPy 1.25.4. Original files are read-only; output must be in artifacts.
The runtime verifies every source SHA-256 before reading any indexed payload.
"""
import argparse, hashlib, io, json, struct
from pathlib import Path
import UnityPy

def string(out, value):
    data=value.encode('utf8'); n=len(data)
    while n>=128:out.write(bytes([(n&127)|128])); n>>=7
    out.write(bytes([n]));out.write(data)

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--game-root',type=Path,required=True);p.add_argument('--output',type=Path,required=True);args=p.parse_args()
    repo=Path(__file__).resolve().parents[1];out=args.output.resolve()
    if not out.is_relative_to(repo/'artifacts'):p.error('Output must be inside artifacts')
    data=args.game_root.resolve()/'Mortal_Data';names=['globalgamemanagers.assets','sharedassets2.assets','level2','Managed/Mortal.Core.dll','Managed/Mortal.Story.dll','Managed/Mortal.Free.dll']
    raw={name:(data/name).read_bytes() for name in names};env={name:UnityPy.load(raw[name]) for name in names[:3]};objects={name:{o.path_id:o for o in e.objects} for name,e in env.items()}
    scripts=objects[names[0]]
    def header(file,obj):
        b=obj.get_raw_data();fid,pid=struct.unpack_from('<iq',b,16);n=struct.unpack_from('<i',b,28)[0]
        assert 0<=n<=4096 and 32+n<=len(b)
        af=obj.assets_file;target=file if fid==0 else Path(af.externals[fid-1].path).name
        assert target==names[0];s=scripts[pid].read_typetree()
        return s['m_Namespace']+'.'+s['m_ClassName'],(32+n+3)&~3
    types=['Mortal.Story.ConditionResultConfig','Mortal.Story.SwitchResultConfig','Mortal.Core.FlagCollectionData','Mortal.Core.ConditionResultData','Mortal.Story.SwitchResultData','Mortal.Core.StatGroupVariable','Mortal.Core.FlagData','Mortal.Core.GameStat','Mortal.Core.RelationshipStat','Mortal.Core.GameTimeData','Mortal.Core.Book','Mortal.Core.Misc','Mortal.Core.SpecialItem','Mortal.Core.PlayerTalentData','Mortal.Core.UpgradeItemData','Mortal.Core.BookDataCollection','Mortal.Core.LibraryItemData']
    rows=[];owners={}
    for file in names[:3]:
        for obj in objects[file].values():
            if obj.type.name!='MonoBehaviour':continue
            try:t,start=header(file,obj)
            except (AssertionError,KeyError):continue
            if file=='level2' and t in ['Mortal.Story.CheckPointManager','Mortal.Story.StatModifyManager']:
                assert t not in owners;owners[t]=(obj,start)
            if file!='level2' and t in types:
                assert raw[file][obj.byte_start:obj.byte_start+obj.byte_size]==obj.get_raw_data()
                rows.append((names.index(file),obj.path_id,obj.byte_start,obj.byte_size,types.index(t)))
    cp,start=owners['Mortal.Story.CheckPointManager'];b=cp.get_raw_data();at=start+12;n=struct.unpack_from('<i',b,at)[0];assert 0<n<=16;at+=4
    def pointer(fid,pid,owner):
        target=owner.assets_file.externals[fid-1].path if fid else 'level2'
        assert target=='sharedassets2.assets';return (1,pid)
    switches=[pointer(*struct.unpack_from('<iq',b,at+12*i),cp) for i in range(n)];at+=12*n
    condition=pointer(*struct.unpack_from('<iq',b,at),cp);at+=24;assert at==len(b)
    sm,start=owners['Mortal.Story.StatModifyManager'];b=sm.get_raw_data();assert len(b)-start==32
    reset=pointer(*struct.unpack_from('<iq',b,len(b)-12),sm)
    roots=[condition,*switches,reset]
    profile=io.BytesIO();profile.write(b'DMD1');profile.write(struct.pack('<i',len(names)))
    for name in names:
        string(profile,name);profile.write(struct.pack('<q',len(raw[name])));profile.write(hashlib.sha256(raw[name]).digest())
        externals=next(iter(objects[name].values())).assets_file.externals if name in objects else []
        profile.write(struct.pack('<i',len(externals)))
        for external in externals:
            target=Path(external.path).name;profile.write(struct.pack('<i',names.index(target) if target in names else -1))
    profile.write(struct.pack('<i',len(types)))
    for t in types:string(profile,t)
    profile.write(struct.pack('<i',len(rows)))
    for row in sorted(rows):profile.write(struct.pack('<BqqiB',*row))
    profile.write(struct.pack('<i',len(roots)))
    for fid,pid in roots:profile.write(struct.pack('<Bq',fid,pid))
    for name in names:assert (data/name).read_bytes()==raw[name]
    out.parent.mkdir(parents=True,exist_ok=True);out.write_bytes(profile.getvalue())
    receipt={'format':'DMD1','entries':len(rows),'bytes':out.stat().st_size,'sha256':hashlib.sha256(out.read_bytes()).hexdigest(),'files':{n:{'bytes':len(raw[n]),'sha256':hashlib.sha256(raw[n]).hexdigest()} for n in names},'root_count':len(roots),'contains_game_payloads':False,'sources_unchanged':True}
    out.with_suffix('.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))
if __name__=='__main__':main()
