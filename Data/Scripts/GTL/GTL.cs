using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game;

using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;

using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

using VRageMath;

using ProtoBuf;

//#pragma warning disable IDE0018 // SE don't like this
namespace GrindToLearn3
{
	class ModData
	{
		public static string SPLIT_PROGRAM;
		private static readonly string SPLIT_PROGRAM_VARIABLE = "GTL3_SPLIT_PROGRAM";
		private static readonly string SPLIT_PROGRAM_NAME = "SPLIT_PROGRAM.txt";

		public static void Load(IMyModContext myMod)
		{	
			string s;

			if(MyAPIGateway.Session.IsServer)
			{
				// XXX Temporary disabled to help players with the 'Signal' update
				//SPLIT_PROGRAM = LoadFromWorld(SPLIT_PROGRAM_NAME);

				if(SPLIT_PROGRAM == null)
					SPLIT_PROGRAM = LoadTextFileFromMod(SPLIT_PROGRAM_NAME, myMod);

				s = Utilities.Base64Encode(SPLIT_PROGRAM);
				MyAPIGateway.Utilities.SetVariable<string>(SPLIT_PROGRAM_VARIABLE, s);
			}

			// server and client code
			s = null;
			if(!MyAPIGateway.Utilities.GetVariable<string>(SPLIT_PROGRAM_VARIABLE, out s) || s == null)
				throw new Exception("Where is the variable??");

			SPLIT_PROGRAM = Utilities.Base64Decode(s);
		}

		public static void Save()
		{	//SaveToWorld(SPLIT_PROGRAM_NAME, SPLIT_PROGRAM);
		}

		private static string LoadFromWorld(string filename)
		{
			if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(filename, typeof(ModData))) return null;

			try
			{	BinaryReader br = MyAPIGateway.Utilities.ReadBinaryFileInWorldStorage(filename, typeof(ModData));
				return new string(br.ReadChars((int)br.BaseStream.Length));
			}
			catch(Exception e)
			{	Utilities.LogException("LoadFromWorld error: ", filename, e);
				return null;
			}
		}

		private static void SaveToWorld(string filename, string data)
		{
			try
			{	BinaryWriter bw = MyAPIGateway.Utilities.WriteBinaryFileInWorldStorage(filename, typeof(ModData));
				bw.Write(data.ToArray());
				bw.Close();
			}
			catch(Exception e)
			{	Utilities.LogException("SaveToWorld error: ", filename, e);
			}
		}
		private static string LoadTextFileFromMod(string name, IMyModContext myMod)
		{
			BinaryReader reader = MyAPIGateway.Utilities.ReadBinaryFileInModLocation("Data/"+name, myMod.ModItem);
			return new string(reader.ReadChars((int)reader.BaseStream.Length));
		}
	}

	public class Settings
	{
		public bool enableGrindToLearn = true;
		public bool enableBlueprints = true;
		public int learnRadius = 15;
		public bool blueprintsGPS = false;
		public float chanceMultiplier = 2.0f;
		public float chanceMultiplierSmallGrid = 0.5f;
		public float chanceKeen = 1.0f;
		public float chanceMods = 0.5f;
		public bool strongEncryption = false;

		public static Settings Load()
		{	Settings s = Utilities.DeserializeFromWorldStorage<Settings>(false, "settings.xml", typeof(Settings));
			if(s == null) return new Settings();

			return s;
		}

		public void Save()
		{	if(!MyAPIGateway.Session.IsServer) return;
			
			Utilities.SerializeToWorldStorage(false, "settings.xml", typeof(Settings), this);
		}
	}

	class Utilities
	{
		public static readonly Random random = new Random((int)DateTime.Now.Ticks);

		public static void SerializeToWorldStorage<T>(bool binary, string filename, Type type, T data)
		{
			try
			{	if(MyAPIGateway.Utilities.FileExistsInWorldStorage(filename, type))
					MyAPIGateway.Utilities.DeleteFileInWorldStorage(filename, type);

				if(binary)
					using (var writer = MyAPIGateway.Utilities.WriteBinaryFileInWorldStorage(filename, type))
						writer.Write(MyAPIGateway.Utilities.SerializeToBinary(data));
				else
					using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(filename, type))
						writer.Write(MyAPIGateway.Utilities.SerializeToXML(data));
					
			}
			catch (Exception e)
			{	LogException("Error writion file to local storage: ", filename, e);
			}
		}

		public static T DeserializeFromWorldStorage<T>(bool binary, string filename, Type type)
		{	try
			{
				if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(filename, type)) return default(T);

				if(binary)
					using (var reader = MyAPIGateway.Utilities.ReadBinaryFileInWorldStorage(filename, type))
					{
						var file = reader.ReadBytes((int)reader.BaseStream.Length);
						return MyAPIGateway.Utilities.SerializeFromBinary<T>(file);
					}
				else
					using (var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(filename, type))
					{
						var file = reader.ReadToEnd();
						if(string.IsNullOrWhiteSpace(file)) return default(T);

						return MyAPIGateway.Utilities.SerializeFromXML<T>(file);
					}
					
			}
			catch (Exception e)
			{
				LogException("Error reading file from local storage: ", filename, e);
				
				return default(T);
			}
		}

		public static string Base64Encode(string plainText) 
		{
			var bytes = Encoding.UTF8.GetBytes(plainText);
			return Convert.ToBase64String(bytes);
		}

		public static string Base64Decode(string base64Encoded) 
		{
			var bytes = Convert.FromBase64String(base64Encoded);
			return Encoding.UTF8.GetString(bytes);
		}

		public static T Limit<T>(T value, T minimal, T maximal) where T : IComparable<T>
		{
			if (value.CompareTo(minimal) < 0) return minimal;
			if (value.CompareTo(maximal) > 0) return maximal;
			return value;
		}

 		private static int Hex_to_dec(int c)
		{
			if(c >= '0' && c <= '9') return c - '0';
			if(c >= 'A' && c <= 'F') return c - 'A' + 10;
			if(c >= 'a' && c <= 'f') return c - 'a' + 10;

			return -1;
		}

		public static List<byte> FromHex(string s) // Ignores all non-hex chars
		{
			List<byte> result = new List<byte>(s.Length / 2);

			for(int i = 0; i < s.Length - 1;)
			{
				int hi = Hex_to_dec(s[i++]);
				if(hi < 0) continue;

				int lo = Hex_to_dec(s[i++]);
				if(lo < 0) continue; // Ignore single hex digit

				int b = (hi << 4) | lo;
				result.Add( (byte)b );
			}

			return result;
		}

		public static string ToHex(byte[] data)
		{	return BitConverter.ToString(data).Replace("-", "");
		}

		public static bool EndsWith(StringBuilder sb, string value)
		{
			if (sb == null) return false;

			int len = sb.Length - value.Length;
			if (len < 0) return false;

			return sb.ToString(len, value.Length).Equals(value); 
		}

		public static IEnumerable<List<string>> MyTextDecoder(string text)
		{
			string[] lines = text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

			foreach (string x in lines)
			{
				string line = x.Trim();
				if (line.Length == 0) continue;

				// remove comments
				var comment = line.IndexOf('#');
				if (comment != -1) line = line.Substring(0, comment).Trim();
				if (line.Length == 0) continue;

				// split line to words
				var words = line.Split(' ').Select(s => s.Trim()).ToList();

				yield return words;
			}
		}

		public static void Log(string s)
		{	
			MyLog.Default.WriteLine(GTL.TAG+" "+s);
		}

		public static void LogException(string text, string subject, Exception e)
		{	MyLog.Default.WriteLine($"{GTL.TAG} {text} '{subject}' \n{e.Message}\n\n{e.StackTrace}");
		}

		public static void Shuffle<T>(IList<T> list)
		{	
			int n = list.Count;
			while (n > 1)
			{
				--n;
				int k = random.Next(n + 1);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}
	}

	class Localization
	{	public static readonly string blueprint = "Blueprint";
		public static readonly string blueprints = "Blueprints!";
		public static readonly string dataAdded = "The data has been decrypted!";
		public static readonly string cannotHack = "Error: can't hack this!";
		public static readonly string noData = "There is no encrypted data!";
		public static readonly string blueprintList = "This datapad contains blueprints:";
		public static readonly string useGrinder = "Use your hand grinder to hack the device";
	}

	class SplitProgram
	{
		class KV
		{
			public string key;
			public MyDefinitionId value;

			public KV(string k, MyDefinitionId v)
			{	this.key = k;
				this.value = v;
			}
		}

		static List<Regex> ToRegexes(List<string> strings)
		{	List<Regex> result = new List<Regex>();

			foreach(var s in strings)
				result.Add(new Regex(s));

			return result;
		}

		static List<Regex> ToRegex(string s)
		{	List<Regex> result = new List<Regex>();
			
			result.Add(new Regex(s));
			
			return result;
		}

		static void Split(
			List<KV> kv_list,
			List<Regex> regex_list,

			out List<KV> matched,
			out List<KV> notMatched
			)
		{
			matched = new List<KV>();
			notMatched = new List<KV>();

			foreach(var kv in kv_list)
			{
				bool match = false;
				foreach(var regex in regex_list)
				{	if(regex.IsMatch(kv.key))
					{	match = true;
						break;
					}
				}

				if(match) matched.Add(kv);
				else notMatched.Add(kv);
			}
		}

		public static void Run(
			HashSet<string> modNames,
			List<MyDefinitionId> blocks,

			string splitProgram,

			out List<List<MyDefinitionId>> result,
			out HashSet<MyDefinitionId> disable,
			out HashSet<MyDefinitionId> unlock,
			out Dictionary<MyDefinitionId, float> changeChance)
		{
			result = new List<List<MyDefinitionId>>();
			disable = new HashSet<MyDefinitionId>();
			unlock = new HashSet<MyDefinitionId>();
			changeChance = new Dictionary<MyDefinitionId, float>();

			Dictionary<string, string> defines = new Dictionary<string, string>();

			List<KV> current = new List<KV>();

			List<KV> matched, notMatched;

			foreach(var block in blocks) current.Add(new KV(block.ToString(), block));

			bool skip = false;

			foreach(var tokens in Utilities.MyTextDecoder(splitProgram))
			{
				string command = tokens[0];

				// macroses

				string redefined;
				if(defines.TryGetValue(command, out redefined))
				{	tokens[0] = redefined;
					command = redefined;
				}

				// commands

				if(command.Equals("IFMOD"))
				{	Utilities.Log(string.Join(" ", tokens));
					
					if(tokens.Count != 2) throw new Exception("Syntax: IFMOD mod_name_regex");
					
					Regex re = new Regex(tokens[1]);

					skip = true;

					foreach(var name in modNames)
						if(re.IsMatch(name))
							skip = false;

					continue;
				}
				
				Utilities.Log((skip ? "SKIP: " : "LINE: ") + string.Join(" ", tokens));
				if(skip) continue;

				if(command.Equals("DEFINE"))
				{	if(tokens.Count != 3) throw new Exception("Syntax: DEFINE identifier value");

					defines[tokens[1]] = tokens[2];
				}
				else if(command.Equals("DUMP")) // Dump current state to log
				{
					foreach(var e in current) Utilities.Log("DUMP: (" + e.key + ") " + e.value);
				}
				else if(command.Equals("IGNORE")) // Do not process these blocks
				{	if(tokens.Count < 2) throw new Exception("Syntax: IGNORE regex_list");

					tokens.RemoveRange(0, 1);
					var re = ToRegexes(tokens);

					Split(current, re, out matched, out notMatched);

					current = notMatched;
				}
				else if(command.Equals("DISABLE")) // Make non public
				{	if(tokens.Count < 2) throw new Exception("Syntax: DISABLE regex_list");

					tokens.RemoveRange(0, 1);
					var re = ToRegexes(tokens);

					Split(current, re, out matched, out notMatched);

					foreach(var m in matched) disable.Add(m.value);
				}
				else if(command.Equals("UNLOCK")) // Unlock for every player
				{	if(tokens.Count < 2) throw new Exception("Syntax: UNLOCK regex_list");

					tokens.RemoveRange(0, 1);
					var re = ToRegexes(tokens);

					Split(current, re, out matched, out notMatched);

					foreach(var m in matched) unlock.Add(m.value);
				}
				else if(command == "MATCH") // Run replacers only on matching subset
				{
					if(tokens.Count < 3) throw new Exception("Syntax: MATCH regex regex_list");

					var re = ToRegex(tokens[1]);

					tokens.RemoveRange(0, 2);

					Split(current, re, out matched, out notMatched);

					RunReplacers(matched, tokens);
				}
				else if(command.Equals("GROUP")) // Create group from matching subset
				{
					if(tokens.Count < 2) throw new Exception("Syntax: GROUP regex_list");

					tokens.RemoveRange(0, 1);

					var re = ToRegexes(tokens);

					Split(current, re, out matched, out notMatched);

					current = notMatched;

					List<MyDefinitionId> a = new List<MyDefinitionId>();
					foreach(var m in matched) a.Add(m.value);
					
					result.Add(a);
				}
				else if(command.Equals("CHANCE")) // Chance to spawn
				{	
					float chance;
					
					if(tokens.Count < 3 ||
						!float.TryParse(tokens[1], out chance)
						) throw new Exception("Syntax: CHANCE float regex_list");

					tokens.RemoveRange(0, 2);
					var re = ToRegexes(tokens);

					Split(current, re, out matched, out notMatched);

					foreach(var m in matched) changeChance.Add(m.value, chance);
				}
				else // regex_list
				{
					RunReplacers(current, tokens);
				}
			}

			Dictionary<string, List<MyDefinitionId>> remainder = new Dictionary<string, List<MyDefinitionId>>();

			foreach(var e in current)
			{
				List<MyDefinitionId> rrr;

				if(!remainder.TryGetValue(e.key, out rrr))
				{	rrr = new List<MyDefinitionId>();
					remainder[e.key] = rrr; 
				}
				rrr.Add(e.value);
			}

			foreach(var element in remainder)
			{	result.Add(element.Value);
				
				foreach(var v in element.Value) Utilities.Log("REMAINDER: (" + element.Key + ") " + v);
			}
		}

		private static void RunReplacers(List<KV> list, List<string> definition)
		{
			foreach (var def in definition)
			{
				string[] what = def.Split('=');
				if (what.Length != 2)
				{	Utilities.Log("You probably forgot =");
					throw new Exception("Syntax: regex=replacer");
				}

				Regex re = new Regex(what[0]);

				bool replaced = false;

				foreach (var e in list)
				{
					string old = e.key;
					e.key = re.Replace(e.key, what[1]);

					if(e.key != old)
					{	replaced = true;
						// Utilities.Log("REPLACED "+old+" -> "+e.k); // log spam
					}
				}

				if(!replaced) Utilities.Log("WARNING: no match for " + what[0]);
			}
		}
	}

	[ProtoContract]
	public class Datapads
	{
		[ProtoContract]
		public class Datapad
		{
			[ProtoMember(1)] public string key;
			[ProtoMember(2)] public SerializableDefinitionId id;
			[ProtoMember(3)] public DateTime created;
		}

		private Dictionary<string, Datapad> existing =
			new Dictionary<string, Datapad>();

		[ProtoMember(42)]
		public List<Datapad> workarround;

		public string CreateKeyFor(MyDefinitionId id)
		{	
			string key = Guid.NewGuid().ToString(); // 122 bits of strong entropy
			key = key.Replace("-", "").ToUpper();

			existing[key] = new Datapad { key = key, id = id, created = DateTime.Now };

			return key;
		}
		public bool TryGet(string key, out MyDefinitionId id)
		{	
			Datapad d;

			if(!existing.TryGetValue(key, out d))
			{	id = new MyDefinitionId();
				return false;
			}
			id = d.id;

			existing.Remove(key); // Not needed anymore

			return true;
		}
		public void Save()
		{
			if (!MyAPIGateway.Session.IsServer) return;

			DeleteExpired();

			workarround = existing.Values.ToList();

			Utilities.SerializeToWorldStorage(true, "datapads.bin", typeof(Datapads), this);

			workarround = null;
		}

		private void DeleteExpired()
		{
			var exp = DateTime.Now.AddDays(-3);

			Dictionary<string, Datapad> notExpired = new Dictionary<string, Datapad>();
			foreach (var element in existing)
			{
				if (exp.CompareTo(element.Value.created) < 0)
				{
					notExpired.Add(element.Key, element.Value);
				}
			}
			existing = notExpired;
		}

		public static Datapads Load()
		{
			var r = Utilities.DeserializeFromWorldStorage<Datapads>(true, "datapads.bin", typeof(Datapads));
			if(r == null) r = new Datapads();

			if(r.workarround != null)
			{	foreach(var dp in r.workarround)
					r.existing[dp.key] = dp;

				r.workarround = null;
			}

			r.DeleteExpired();

			return r;
		}
	}

	[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
	public class GTL : MySessionComponentBase
	{
		private const ushort ChatCommandChannel = 52341;

		public static readonly string TAG = "GrindToLearn3";
		public override string ToString() { return TAG; }

		Random random => Utilities.random;

		private static Settings settings;

		private static Datapads datapads;

		private static void Log(string s) { Utilities.Log(s); }

		private static void Dump<T>(string prefix, List<T> list)
		{
			StringBuilder b = new StringBuilder();
			b.Append(TAG);
			b.Append(' ');
			b.Append(prefix);
			b.Append(':');
			
			foreach(var v in list)
			{	string s = v.ToString();

				b.Append(' ');

				if(s.StartsWith("MyObjectBuilder_"))
					b.Append(s, 16, s.Length - 16);
				else
					b.Append(s);
			}

			MyLog.Default.WriteLine(b.ToString());
		}

		private static void DumpResearch(string message)
		{	
			Log(message + " UnlockedByGroups");

			foreach (MyResearchBlockDefinition def in MyDefinitionManager.Static.GetResearchBlockDefinitions())
				Dump(def.Id.ToString(), def.UnlockedByGroups.ToList());

			Log(message + " MyResearchGroupDefinition");

			foreach (MyResearchGroupDefinition def in MyDefinitionManager.Static.GetResearchGroupDefinitions())
				Dump(def.Id.ToString(), def.Members.ToList());
		}

		public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
		{
			MyAPIGateway.Session.DamageSystem.RegisterDestroyHandler(0, DestroyHandler);
			MyAPIGateway.Utilities.MessageEnteredSender += OnMessageEnteredSender;

			if(MyAPIGateway.Session.IsServer)
				MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(ChatCommandChannel, OnChatCommandMessage);
			
			MyVisualScriptLogicProvider.PlayerConnected += OnPlayerConnected;
			MyVisualScriptLogicProvider.PrefabSpawnedDetailed += OnPrefabSpawned;
		}

		protected override void UnloadData()
		{
			MyAPIGateway.Utilities.MessageEnteredSender -= OnMessageEnteredSender;

			if(MyAPIGateway.Session.IsServer)
				MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(ChatCommandChannel, OnChatCommandMessage);

			MyVisualScriptLogicProvider.PlayerConnected -= OnPlayerConnected;
			MyVisualScriptLogicProvider.PrefabSpawnedDetailed -= OnPrefabSpawned;
		}

		public override void SaveData()
		{
			Log("SaveData");

			settings?.Save();
			datapads?.Save();

			ModData.Save();

			Log("/SaveData");
		}

		private enum BLOCK_SOURCE
		{	KEEN,
			GTL,
			MOD
		}

		private BLOCK_SOURCE Which(MyCubeBlockDefinition def)
		{
			string modName = def.Context.ModName;

			if(modName == null || modName.Length == 0) return BLOCK_SOURCE.KEEN;

			string currentMod = ModContext.ModName;

			if(currentMod.Equals(modName)) return BLOCK_SOURCE.GTL;

			return BLOCK_SOURCE.MOD;
		}

		private bool HasPublicBlocks(List<MyDefinitionId> list)
		{
			foreach(var block in list)
			{	var def = MyDefinitionManager.Static.GetCubeBlockDefinition(block);
				if(def.Public) return true;
			}
			return false;
		}

		private bool HasResearchBlocks(List<MyDefinitionId> list)
		{
			foreach(var block in list)
			{	var def = MyDefinitionManager.Static.GetResearchBlock(block);
					if (def != null) return true;
			}
			return false;
		}

		private class Research
		{
			public MyDefinitionId firstBlock;
			public StringBuilder whatUnlocks = new StringBuilder();
			public float chanceToSpawn;
			public bool unlockOnPlayerConnected;
		}

		private List<Research> researchList = new List<Research>();
		private Dictionary<MyDefinitionId, MyDefinitionId> unlockers = new Dictionary<MyDefinitionId, MyDefinitionId>(MyDefinitionId.Comparer);
		private MyDefinitionId datapadLocked;

		public override void LoadData()
		{
			Log("LoadData");

			//DumpResearch("ON START");

			ModData.Load(ModContext);
			settings = Settings.Load();

			if(settings.strongEncryption) datapads = Datapads.Load();

			datapadLocked = new MyDefinitionId(typeof(MyObjectBuilder_Datapad), "DatapadLocked");

			HashSet<MyDefinitionId> allBlocks = new HashSet<MyDefinitionId>();
			HashSet<MyDefinitionId> modsBlocks = new HashSet<MyDefinitionId>();
			HashSet<MyDefinitionId> keenBlocks = new HashSet<MyDefinitionId>();

			HashSet<string> modNames = new HashSet<string>();

			Dictionary<MyDefinitionId, string> blockNames = new Dictionary<MyDefinitionId, string>();

			foreach (var def in MyDefinitionManager.Static.GetAllDefinitions().OfType<MyCubeBlockDefinition>())
			{
				string modName = def.Context.ModName;
				if(modName != null) modNames.Add(modName);

				blockNames.Add(def.Id, def.DisplayNameText);

				var w = Which(def);

				switch(w)
				{	case BLOCK_SOURCE.GTL: continue;					
					case BLOCK_SOURCE.KEEN: keenBlocks.Add(def.Id); break;
					case BLOCK_SOURCE.MOD: modsBlocks.Add(def.Id); break;
				}

				allBlocks.Add(def.Id);
			}

			foreach(var name in modNames) Log("MOD NAME "+name);

			Log("RUNNING SPLIT PROGRAM");
			
			List<List<MyDefinitionId>> groups;
			HashSet<MyDefinitionId> disableThis;
			HashSet<MyDefinitionId> unlockThis;
			Dictionary<MyDefinitionId, float> changeChance;

			SplitProgram.Run(modNames, allBlocks.ToList(), ModData.SPLIT_PROGRAM,
				out groups, out disableThis, out unlockThis, out changeChance);

			// disable blocks
			foreach(var block in disableThis)
			{	var def = MyDefinitionManager.Static.GetCubeBlockDefinition(block);
				def.Public = false;
			}
			disableThis = null;

			Log("CLEARING RESEARCH");

			var plushie = new MyDefinitionId(typeof(MyObjectBuilder_CubeBlock), "EngineerPlushie");
			var sabiroid = new MyDefinitionId(typeof(MyObjectBuilder_CubeBlock), "SabiroidPlushie");
			groups.Insert(0, new List<MyDefinitionId>() { plushie, sabiroid }); // Avoid game crash

			foreach (var def in MyDefinitionManager.Static.GetResearchBlockDefinitions())
			{	def.UnlockedByGroups = new string[] {};
			}
			foreach (var def in MyDefinitionManager.Static.GetResearchGroupDefinitions())
			{
				if ("0" == def.Id.SubtypeId.ToString())
				{
					def.Members = new SerializableDefinitionId[] { plushie, sabiroid }; // Avoid game crash
					continue;
				}
				
				def.Members = new SerializableDefinitionId[] { };
			}

			Log("groups.Count (1) " + groups.Count);

			List<List<MyDefinitionId>> new_groups = new List<List<MyDefinitionId>>();

			for(int i = 0; i < groups.Count; ++i)
			{
				var group = groups[i];

				bool p = HasPublicBlocks(group);
				bool r = HasResearchBlocks(group);

				if(i == 0 || (p && r))
				{	new_groups.Add(group);
				}
				else
				{	if(!p) Dump("IGNORING (no public blocks)", group);
					else if(!r) Dump("IGNORING (no research blocks)", group);
				}
			}

			groups = new_groups;
			new_groups = null;

			Log("groups.Count (2) " + groups.Count);

			HashSet<MyDefinitionId> used = new HashSet<MyDefinitionId>();
			foreach(var group in groups)
				foreach(var block in group)
					used.Add(block);

			bool warning = false;

			foreach(var def in MyDefinitionManager.Static.GetAllDefinitions().OfType<MyCubeBlockDefinition>())
			{	if(!def.Public) continue;

				if(Which(def) == BLOCK_SOURCE.GTL) continue;

				if(used.Contains(def.Id)) continue;

				if(!warning)
				{	warning = true;
					Log("WARNING: blocks not included in any group:");
				}
				Log(def.Context.ModName + ": " + def.Id.ToString());
			}

			Log("REDEFINING RESEARCH");

			researchList.Add(new Research()); // group 0 should not be used

			for (int i = 1; i < groups.Count; ++i) // group 0 is Plushie
			{
				Research research = new Research();
				researchList.Add(research);

				string unlockerGroup = "U" + i;

				MyResearchGroupDefinition unlockerRG = MyDefinitionManager.Static.GetResearchGroup(unlockerGroup);
				if(unlockerRG == null)
				{
					Log("ERROR: GetResearchGroup (unlocker) " + i);
					continue;
				}

				MyDefinitionId unlockerId = new MyDefinitionId(typeof(MyObjectBuilder_CubeBlock), "UnlockerU" + i);
				MyCubeBlockDefinition unlocker = MyDefinitionManager.Static.GetCubeBlockDefinition(unlockerId);

				if(unlocker == null)
				{
					Log("ERROR: GetCubeBlockDefinition (unlocker) " + i);
					continue;
				}

				MyDefinitionId researchGroupId = new MyDefinitionId(typeof(MyObjectBuilder_ResearchGroupDefinition), unlockerGroup);
				research.firstBlock = groups[i][0];

				foreach (var block in groups[i])
				{
					MyResearchBlockDefinition def = MyDefinitionManager.Static.GetResearchBlock(block);
					if (def == null)
					{
						Log("ERROR: GetResearchBlock " + block);
						continue;
					}

					def.UnlockedByGroups = new string[] { unlockerGroup };
					unlockers[block] = researchGroupId;
					
					if(unlockThis.Contains(block)) research.unlockOnPlayerConnected = true;

					float blockChance;
					if(changeChance.TryGetValue(block, out blockChance))
						research.chanceToSpawn += blockChance;
					else
					{	if(keenBlocks.Contains(block)) research.chanceToSpawn += settings.chanceKeen;
						if(modsBlocks.Contains(block)) research.chanceToSpawn += settings.chanceMods;
					}

					if(research.whatUnlocks.Length <  500)
					{	
						string name = blockNames[block];
						if(!research.whatUnlocks.ToString().Contains(name)) // we can be slow here
						{
							if(research.whatUnlocks.Length > 0) research.whatUnlocks.Append("\n");
							research.whatUnlocks.Append(name);
						}
					}
					else
					{	if(!Utilities.EndsWith(research.whatUnlocks, "...")) research.whatUnlocks.Append("\n...");
					}
				}

				research.chanceToSpawn /= groups[i].Count; // I forget this last time, sorry :)

				unlockerRG.Members = new SerializableDefinitionId[] { unlockerId };
			}

			foreach(var r in researchList)
				Log(r.firstBlock + " chance " + r.chanceToSpawn);

			//DumpResearch("REDEFINED");
			
			Log("/LoadData");
		}

		public override void BeforeStart()
		{
			//MyVisualScriptLogicProvider.ResearchListWhitelist(true); // this call locks all mod blocks ...
			MyAPIGateway.Session.SessionSettings.EnableResearch = true;
				// We are useless without the "progression" enabled.
		}

		private bool MyResearchUnlock(MyDefinitionId blockId, long playerId)
		{
			Log("MyResearchUnlock " + blockId + " " + playerId);

			MyDefinitionId researchGroupId;
			if (!unlockers.TryGetValue(blockId, out researchGroupId))
			{
				Log("ERROR: Research group not found for " + blockId);
				return false;
			}

			Log("UnlockResearchGroup playerId=" + playerId + " researchGroupId=" + researchGroupId);
			MyVisualScriptLogicProvider.PlayerResearchUnlock(playerId, researchGroupId);

			return true;
		}

		private void OnMessageEnteredSender(ulong sender, string messageText, ref bool sendToOthers)
		{
			if(!IsGTLCommand(messageText)) return;

			sendToOthers = false;
			if(messageText.Length > 512)
			{
				MyVisualScriptLogicProvider.ShowNotificationLocal("GTL command is too long.", 6000, "White");
				return;
			}

			if(MyAPIGateway.Session.IsServer)
			{
				ProcessChatCommand(sender, messageText);
				return;
			}

			byte[] message = Encoding.UTF8.GetBytes(messageText);
			if(!MyAPIGateway.Multiplayer.SendMessageToServer(ChatCommandChannel, message, true))
				MyVisualScriptLogicProvider.ShowNotificationLocal("GTL: Could not send command to server.", 6000, "White");
		}

		private void OnChatCommandMessage(ushort channel, byte[] message, ulong sender, bool fromServer)
		{
			if(channel != ChatCommandChannel || fromServer || message == null || message.Length > 512) return;

			ProcessChatCommand(sender, Encoding.UTF8.GetString(message));
		}

		private static bool IsGTLCommand(string message)
		{
			if(string.IsNullOrWhiteSpace(message)) return false;

			string text = message.TrimStart();
			return text.StartsWith("/gtl", StringComparison.OrdinalIgnoreCase) &&
				(text.Length == 4 || char.IsWhiteSpace(text[4]));
		}

		private void ProcessChatCommand(ulong sender, string message)
		{
			var players = new List<IMyPlayer>();
			MyAPIGateway.Players.GetPlayers(players, candidate => candidate.SteamUserId == sender);

			if(players.Count == 0)
			{
				Log("ERROR: GTL command sender not found: " + sender);
				return;
			}

			IMyPlayer player = players[0];
			string arguments = message.Trim().Substring(4).Trim();
			int separator = arguments.IndexOfAny(new char[] { ' ', '\t', '\r', '\n' });
			string action = separator < 0 ? arguments : arguments.Substring(0, separator);
			string blockName = separator < 0 ? string.Empty : arguments.Substring(separator).Trim();

			if(action.Equals("help", StringComparison.OrdinalIgnoreCase) ||
				string.IsNullOrEmpty(action))
			{
				RespondToPlayer(player.IdentityId, "GTL: /gtl learn <block> or /gtl unlearn <block>. Use subtype or Type/Subtype; this changes the block's whole research group.");
				return;
			}

			bool learn = action.Equals("learn", StringComparison.OrdinalIgnoreCase) ||
				action.Equals("unlock", StringComparison.OrdinalIgnoreCase);
			bool unlearn = action.Equals("unlearn", StringComparison.OrdinalIgnoreCase) ||
				action.Equals("forget", StringComparison.OrdinalIgnoreCase) ||
				action.Equals("lock", StringComparison.OrdinalIgnoreCase);

			if(!learn && !unlearn)
			{
				RespondToPlayer(player.IdentityId, "Unknown GTL command. Use /gtl help.");
				return;
			}

			if(string.IsNullOrEmpty(blockName))
			{
				RespondToPlayer(player.IdentityId, "Specify a block subtype or Type/Subtype ID. Use /gtl help.");
				return;
			}

			List<MyDefinitionId> matches = new List<MyDefinitionId>();
			foreach(var block in unlockers.Keys)
			{
				var definition = MyDefinitionManager.Static.GetCubeBlockDefinition(block);
				string subtype = block.SubtypeName.ToString();

				if(subtype.Equals(blockName, StringComparison.OrdinalIgnoreCase) ||
					block.ToString().Equals(blockName, StringComparison.OrdinalIgnoreCase) ||
					(definition != null && definition.DisplayNameText.Equals(blockName, StringComparison.OrdinalIgnoreCase)))
					matches.Add(block);
			}

			if(matches.Count == 0)
			{
				RespondToPlayer(player.IdentityId, "No learned block group matches '" + blockName + "'.");
				return;
			}

			if(matches.Count > 1)
			{
				StringBuilder choices = new StringBuilder("Ambiguous block. Use Type/Subtype: ");
				int count = Math.Min(matches.Count, 5);
				for(int i = 0; i < count; ++i)
				{
					if(i > 0) choices.Append(", ");
					choices.Append(matches[i].ToString());
				}
				if(matches.Count > count) choices.Append(", ...");

				RespondToPlayer(player.IdentityId, choices.ToString());
				return;
			}

			MyDefinitionId researchGroupId;
			if(!unlockers.TryGetValue(matches[0], out researchGroupId))
			{
				RespondToPlayer(player.IdentityId, "No progression research group is registered for that block.");
				return;
			}

			if(learn)
			{
				MyResearchUnlock(matches[0], player.IdentityId);
				RespondToPlayer(player.IdentityId, "Learn request sent for " + matches[0].ToString() + ".");
			}
			else
			{
				MyVisualScriptLogicProvider.PlayerResearchLock(player.IdentityId, researchGroupId);
				RespondToPlayer(player.IdentityId, "Unlearn request sent for " + matches[0].ToString() + ". Space Engineers may retain build access until progression is reset.");
			}
		}

		private static void RespondToPlayer(long playerId, string message)
		{
			MyVisualScriptLogicProvider.ShowNotification(message, 6000, "White", playerId);
		}

		public void DestroyHandler(object target, MyDamageInformation damage)
		{
			var fo = target as MyFloatingObject;
			if(fo != null)
			{	
				var e = MyEntities.GetEntityById(damage.AttackerId);
				
				var character = e as IMyCharacter;
				if(character == null) return;

				// KEEN, why didn't you use some effect here?
				MyParticleEffect effect;
				if(MyParticlesManager.TryCreateParticleEffect("MaterialHit_MoonSoil", fo.WorldMatrix, out effect)) // MaterialHit_Ice ?
				{	effect.UserRadiusMultiplier = 0.4f;
					effect.UserLifeMultiplier = 0.4f;
				}
				//

				var datapad = fo.Item.Content as MyObjectBuilder_Datapad;
				if(datapad == null) return;

				if(datapad.GetId() != datapadLocked)
				{	MyVisualScriptLogicProvider.ShowNotification(Localization.noData, 7000, MyFontEnum.Red);
					MyVisualScriptLogicProvider.PlayHudSoundLocal();
					return;
				}

				// there is a better way convert IMyCharacter to IMyPlayer?
				var players = new List<IMyPlayer>();
				MyAPIGateway.Players.GetPlayers(players, pl => { return pl.Character == character; });

				if(players.Count < 1) return;
				
				var player = players[0];

				if(!ProcessDatapad(player.IdentityId, datapad))
				{	MyVisualScriptLogicProvider.ShowNotification(Localization.cannotHack, 7000, MyFontEnum.Red);
					MyVisualScriptLogicProvider.PlayHudSoundLocal();
					return;
				}

				MyVisualScriptLogicProvider.ShowNotification(Localization.dataAdded, 7000, MyFontEnum.Green);
				MyVisualScriptLogicProvider.PlayHudSoundLocal();
				return;
			}

			if(damage.Type == MyDamageType.Grind)
			{	
				if(!settings.enableGrindToLearn) return;

				IMyAngleGrinder grinder = MyEntities.GetEntityById(damage.AttackerId) as IMyAngleGrinder;

				if (grinder == null) return;

				var slim = target as IMySlimBlock;
					
				if (slim == null) return;
				
				var blockId = slim.BlockDefinition.Id;

				Vector3D center;
				slim.ComputeWorldCenter(out center);

				var sphere = new BoundingSphereD(center, settings.learnRadius);
				var players = new List<IMyPlayer>();

				MyAPIGateway.Players.GetPlayers(players, p =>
					{ return sphere.Contains(p.GetPosition()) != ContainmentType.Disjoint; }
				);
						
				// FIXME no message here :(
				foreach (var player in players)
				{	MyResearchUnlock(blockId, player.IdentityId);
				}
				return;
			}
		}

		public void OnPlayerConnected(long playerId)
		{
			Log("OnPlayerConnected " + playerId);

			foreach(var research in researchList)
			{	if(!research.unlockOnPlayerConnected) continue;

				MyResearchUnlock(research.firstBlock, playerId);
			}

			Log("/OnPlayerConnected");
		}

		private string EncryptResearch(MyDefinitionId id)
		{	
			var bytes = MyAPIGateway.Utilities.SerializeToBinary<SerializableDefinitionId>(id);

			return Utilities.ToHex(bytes);
		}

		private bool DecryptResearch(string encrypted, out MyDefinitionId id)
		{	
			id = new MyDefinitionId();

			byte[] bb = Utilities.FromHex(encrypted).ToArray();

			try
			{	id = MyAPIGateway.Utilities.SerializeFromBinary<SerializableDefinitionId>(bb);
			}
			catch(Exception e)
			{	Log("ERROR: " + e);
				return false;
			}
			return true;
		}

		private int SelectRandomResearch()
		{	
			float total = 0;

			foreach(var research in researchList)
				total += research.chanceToSpawn;

			float r = (float)random.NextDouble() * total;

			int index = 0;
			foreach(var research in researchList)
			{	r -= research.chanceToSpawn;
				if(r < 0) break;
				++index;
			}
			
			return Utilities.Limit(index, 1, researchList.Count-1); // for sure
		}

		private void GenerateDatapadContent(MyObjectBuilder_Datapad dp)
		{
			dp.Name = Localization.blueprint;

			var research = SelectRandomResearch();

			StringBuilder b = new StringBuilder();

			Research rg = researchList[research];

			string key = settings.strongEncryption ? datapads.CreateKeyFor(rg.firstBlock) : EncryptResearch(rg.firstBlock);

			b.Append(key);
			b.Append("\n\n");
			b.Append(Localization.blueprintList);
			b.Append("\n\n");
			if(rg.whatUnlocks != null)
			{	b.Append(rg.whatUnlocks);
				b.Append("\n\n");
			}
			b.Append(Localization.useGrinder);

			dp.Data = b.ToString();
		}

		private bool ProcessDatapad(long ownerId, MyObjectBuilder_Datapad dp)
		{	
			Log("ProcessDatapad "+ownerId);

			string content = dp.Data;
			int nl = content.IndexOf('\n');
			if(nl >= 0) content = content.Substring(0, nl); // I need first line only

			MyDefinitionId blockId;

			if(settings.strongEncryption)
			{
				if(datapads.TryGet(content, out blockId))
				{
					return MyResearchUnlock(blockId, ownerId);
				}
			}
			else
			{	// QUANTIUM SECURITY VULNERABILITY!
				if(DecryptResearch(content, out blockId) && blockId != null)
				{
					return MyResearchUnlock(blockId, ownerId);
				}
			} 
			
			return false;
		}

		private void OnPrefabSpawned(long entityId, string prefabName)
		{
			if (!settings.enableBlueprints) return;

			IMyCubeGrid grid = MyAPIGateway.Entities.GetEntityById(entityId) as IMyCubeGrid;

			if (grid == null) return;
			if (grid.Physics == null) return; // grid is a projection

			List<IMySlimBlock> terminalBlocks = new List<IMySlimBlock>();
			grid.GetBlocks(terminalBlocks, (b) => { return b.FatBlock is IMyTerminalBlock; });

			if (terminalBlocks.Count == 0) return;

			int toSpawn = BlueprintsCount(grid.GridSizeEnum, terminalBlocks.Count);

			if (toSpawn <= 0) return;

			var spawned = SpawnBlueprints(terminalBlocks, toSpawn);

			Log(grid.Name + " - spawned blueprints: " + spawned);

			if (spawned > 0 && settings.blueprintsGPS)
			{
				string name = Localization.blueprints;
				MyVisualScriptLogicProvider.AddGPS(name, "", grid.GetPosition(), Color.LightGreen);
			}
		}

		private int BlueprintsCount(MyCubeSize gridSize, int terminalBlocksCount)
		{
			float chance;
			if (gridSize == MyCubeSize.Large)
				chance = terminalBlocksCount * settings.chanceMultiplier / 100;
			else
				chance = terminalBlocksCount * settings.chanceMultiplierSmallGrid / 100;

			int blueprintsToSpawn = (int)chance;

			chance -= blueprintsToSpawn;
			if (chance > random.NextDouble()) ++blueprintsToSpawn;
			return blueprintsToSpawn;
		}

		private int SpawnBlueprints(List<IMySlimBlock> terminalBlocks, int count)
		{
			Utilities.Shuffle(terminalBlocks);

			int blueprintsSpawned = 0;

			foreach (var block in terminalBlocks)
			{
				var cargo = block.FatBlock as IMyTerminalBlock;
				if (cargo.MarkedForClose) continue; // ?
				if (!cargo.IsWorking) continue; // ?

				IMyInventory inventory = cargo.GetInventory();
				if (inventory == null) continue;

				for (int s = 0; s < count; ++s)
				{
					var datapad = MyObjectBuilderSerializer.CreateNewObject(datapadLocked) as MyObjectBuilder_Datapad;

					if (datapad == null) break;

					if (!inventory.CanItemsBeAdded(1, datapadLocked)) break;

					GenerateDatapadContent(datapad);

					inventory.AddItems(1, datapad);
					
					++blueprintsSpawned;
				}

				if (blueprintsSpawned > 0) break;
			}

			return blueprintsSpawned;
		}
	}
}
