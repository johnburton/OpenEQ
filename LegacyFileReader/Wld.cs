﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using OpenEQ.Common;
using static System.Console;

namespace OpenEQ.LegacyFileReader {
	public class Reference<T> where T : class {
		readonly Wld Wld;
		readonly int Ref;

		public Reference(Wld wld, int @ref) {
			Wld = wld;
			Ref = @ref;
		}

		public string Name => Wld.GetReferenceName(Ref);
		public T Value => Wld.GetReference(Ref) is T tv ? tv : null;

		public override string ToString() {
			switch(Value) {
				case null:
					return $"Ref<{typeof(T).Name}>(unknown)";
				case string s:
					return $"Ret(\"{s}\")";
				case var val:
					return $"Ref<{typeof(T).Name}>({Name}: {val})";
			}
		}
	}
	
	public class Fragment03 {
		public string[] Filenames;

		public override string ToString() => $"Fragment03(Filenames=[{string.Join(", ", Filenames.Select(x => $"'{x}'"))}]";
	}

	public class Fragment04 {
		public uint FrameTime;
		public Reference<Fragment03>[] References;

		public override string ToString() => $"Fragment04(FrameTime={FrameTime}, References=[{string.Join(", ", References.Select(x => x.ToString()))}])";
	}

	public class Fragment05 {
		public Reference<Fragment04> Reference;

		public override string ToString() => $"Fragment05(Reference={Reference})";
	}

	public class Fragment10 {
	}

	public class Fragment11 {
	}

	public class Fragment12 {
	}

	public class Fragment13 {
	}

	public class Fragment15 {
		public Reference<string> Reference;
		public Vec3 Position, Rotation, Scale;

		public override string ToString() => $"Fragment15(Reference={Reference}, Position={Position}, Rotation={Rotation}, Scale={Scale})";
	}

	public class Fragment1B {
		public uint? Attenuation;
		public Vec3 Color;

		public override string ToString() => $"Fragment1B(Attenuation={Attenuation}, Color={Color})";
	}

	public class Fragment1C {
		public Reference<Fragment1B> Reference;
		
		public override string ToString() => $"Fragment1C(Reference={Reference})";
	}

	public class Fragment22 {
	}

	public class Fragment28 {
		public Reference<Fragment1C> Reference;
		public uint Flags;
		public Vec3 Pos;
		public float Radius;

		public override string ToString() => $"Fragment28(Reference={Reference}, Flags=0x{Flags:X}, Pos={Pos}, Radius={Radius})";
	}

	public class Fragment29 {
	}

	public class Fragment2A {
	}

	public class Fragment2D {
	}

	public class Fragment2F {
	}

	public class Fragment30 {
		public Reference<Fragment05> Reference;
		public uint Flags;

		public override string ToString() => $"Fragment30(Reference={Reference}, Flags=0x{Flags:X8})";
	}

	public class Fragment31 {
		public Reference<Fragment30>[] References;

		public override string ToString() => $"Fragment31(References=[{string.Join(", ", References.Select(x => x.ToString()))}])";
	}

	public class Fragment36 {
		public Reference<Fragment31> TextureListReference;
		public Vec3[] Vertices, Normals;
		public Vec2[] TexCoords;
		public (bool Collidable, uint A, uint B, uint C)[] Polygons;
		public (uint Count, uint Index)[] PolyTexs;
	}
	
	public class Fragment37 {
	}
	
	public class Wld {
		static readonly byte[] StringHashKey = { 0x95, 0x3A, 0xC5, 0x2A, 0x95, 0x7A, 0x95, 0x6A };
		
		readonly Stream Fp;
		readonly BinaryReader Br;
		readonly bool NewFormat;

		readonly string StringHash;

		readonly (string Name, object Fragment)[] Fragments;
		
		public Wld(Stream fp) {
			Fp = fp;
			Br = new BinaryReader(fp);
			
			Debug.Assert(Br.ReadUInt32() == 0x54503D02);
			NewFormat = Br.ReadUInt32() != 0x00015500;

			var fragCount = Br.ReadUInt32();
			Fp.Position += 8;
			var stringHashSize = Br.ReadUInt32();
			Fp.Position += 4;

			StringHash = ReadDecodeString((int) stringHashSize);
			
			while(Fp.Position % 4 != 0)
				Fp.Position++;
			
			Fragments = new (string, object)[fragCount];

			for(var i = 0; i < fragCount; ++i) {
				var size = Br.ReadUInt32();
				var type = Br.ReadUInt32();
				var spos = Fp.Position;
				var nr = Br.ReadInt32();
				var name = nr < 0 && type != 0x35 ? GetString(-nr) : null;

				void Add<T>(T obj) {
					WriteLine(obj);
					Fragments[i] = (name, obj);
				}
				
				WriteLine($"Parsing fragment {i}, type 0x{type:X2} with size 0x{size:X} and name '{name}'");

				switch(type) {
					case 0x03: Add(Read03()); break;
					case 0x04: Add(Read04()); break;
					case 0x05: Add(Read05()); break;
					case 0x08: break;
					case 0x09: break;
					case 0x10: Add(Read10()); break;
					case 0x11: Add(Read11()); break;
					case 0x12: Add(Read12()); break;
					case 0x13: Add(Read13()); break;
					case 0x14: break;
					case 0x15: Add(Read15()); break;
					case 0x16: break;
					case 0x1B: Add(Read1B()); break;
					case 0x1C: Add(Read1C()); break;
					case 0x21: break;
					case 0x22: Add(Read22()); break;
					case 0x26: break; // TODO: Figure out -- totally unknown
					case 0x28: Add(Read28()); break;
					case 0x29: Add(Read29()); break;
					case 0x2A: Add(Read2A()); break;
					case 0x2D: Add(Read2D()); break;
					case 0x2F: Add(Read2F()); break;
					case 0x30: Add(Read30()); break;
					case 0x31: Add(Read31()); break;
					case 0x34: break; // TODO: Figure out -- totally unknown
					case 0x35: break;
					case 0x36: Add(Read36()); break;
					case 0x37: Add(Read37()); break;
					default:
						WriteLine($"Unhandled");
						break;
				}
				Debug.Assert(Fp.Position <= spos + size); // Make sure we didn't read past the end of a fragment
				Fp.Position = spos + size;
			}

			/*foreach(var (name, obj) in Fragments) {
				switch(obj) {
					case null: break;
					case Fragment04 f04:
						WriteLine($"0x04 fragment {f04}");
						WriteLine($"- References [{string.Join(", ", f04.References.Select(GetReference))}]");
						break;
					case Fragment15 f15:
						WriteLine($"0x15 fragment {f15}");
						WriteLine($"- Reference {GetReference(f15.Reference)}");
						break;
					case Fragment30 f30:
						WriteLine($"0x30 fragment {f30}");
						WriteLine($"- Reference {GetReference(f30.Reference)}");
						break;
				}
			}*/
		}

		Fragment03 Read03() =>
			new Fragment03 {
				Filenames = Enumerable.Range(0, Br.ReadInt32() + 1).Select(_ => ReadDecodeString(Br.ReadUInt16()).TrimEnd('\0')).ToArray()
			};
		
		Fragment04 Read04() {
			var flags = Br.ReadUInt32();
			var refCount = Br.ReadInt32();
			if((flags & (1 << 2)) != 0)
				Br.ReadUInt32();
			var frameTime = (flags & (1 << 3)) != 0 ? Br.ReadUInt32() : 0;
			var refs = Enumerable.Range(0, refCount).Select(_ => ReadRef<Fragment03>()).ToArray();
			return new Fragment04 {
				FrameTime = frameTime, 
				References = refs
			};
		}
		
		Fragment05 Read05() {
			var reference = ReadRef<Fragment04>();
			Br.ReadUInt32();
			return new Fragment05 { Reference = reference };
		}

		Fragment10 Read10() {
			return new Fragment10();
		}

		Fragment11 Read11() {
			return new Fragment11();
		}

		Fragment12 Read12() {
			return new Fragment12();
		}
		
		Fragment13 Read13() {
			return new Fragment13();
		}
		
		Fragment15 Read15() {
			var reference = ReadRef<string>();
			Br.ReadUInt32();
			Br.ReadUInt32();
			var pos = Br.ReadVec3();
			var rot = Br.ReadVec3();
			var scale = Br.ReadVec3();

			scale = scale.Z > 0.0001 ? new Vec3(scale.Z) : Vec3.One;
			rot = new Vec3(rot.Z / 512 * 360 * MathF.PI / 180, rot.Y / 512 * 360 * MathF.PI / 180, rot.X / 512 * 360 * MathF.PI / 180);
			
			return new Fragment15 {
				Reference = reference, 
				Position = pos, 
				Rotation = rot, 
				Scale = scale
			};
		}

		Fragment1B Read1B() {
			var flags = Br.ReadUInt32();
			Br.ReadUInt32();
			uint? attenuation = null;
			Vec3 color;
			if((flags & (1 << 4)) != 0) {
				if((flags & (1 << 3)) != 0)
					attenuation = Br.ReadUInt32();
				Br.ReadSingle();
				color = Br.ReadVec3();
			} else
				color = new Vec3(Br.ReadSingle());
			return new Fragment1B { Attenuation = attenuation, Color = color };
		}

		Fragment1C Read1C() {
			var reference = ReadRef<Fragment1B>();
			Br.ReadUInt32();
			return new Fragment1C { Reference = reference };
		}

		Fragment22 Read22() {
			return new Fragment22();
		}

		Fragment28 Read28() =>
			new Fragment28 {
				Reference = ReadRef<Fragment1C>(), 
				Pos = Br.ReadVec3(), 
				Radius = Br.ReadSingle()
			};

		Fragment29 Read29() {
			return new Fragment29();
		}
		
		Fragment2A Read2A() {
			return new Fragment2A();
		}

		Fragment2D Read2D() {
			return new Fragment2D();
		}

		Fragment2F Read2F() {
			return new Fragment2F();
		}
		
		Fragment30 Read30() {
			var existenceFlags = Br.ReadUInt32();
			var texFlags = Br.ReadUInt32();
			Br.ReadUInt32();
			Br.ReadSingle();
			Br.ReadSingle();
			if((existenceFlags & (1 << 1)) != 0) {
				Br.ReadUInt32();
				Br.ReadSingle();
			}
			var reference = ReadRef<Fragment05>();
			return new Fragment30 {
				Reference = reference, 
				Flags = texFlags
			};
		}
		
		Fragment31 Read31() {
			Debug.Assert(Br.ReadUInt32() == 0);
			return new Fragment31 {
				References = Enumerable.Range(0, Br.ReadInt32()).Select(_ => ReadRef<Fragment30>()).ToArray()
			};
		}

		Fragment36 Read36() {
			Br.ReadUInt32();
			var texRef = ReadRef<Fragment31>();
			var aniRef = ReadRef<Fragment2F>();
			Br.ReadUInt32();
			Br.ReadInt32();
			var center = Br.ReadVec3();
			Br.ReadUInt32();
			Br.ReadUInt32();
			Br.ReadUInt32();
			var maxDist = Br.ReadSingle();
			var mins = Br.ReadVec3();
			var maxs = Br.ReadVec3();
			var vertCount = Br.ReadUInt16();
			var tcCount = Br.ReadUInt16();
			var normalCount = Br.ReadUInt16();
			var colorCount = Br.ReadUInt16();
			var polyCount = Br.ReadUInt16();
			var vertPieceCount = Br.ReadUInt16();
			var polyTexCount = Br.ReadUInt16();
			var vertTexCount = Br.ReadUInt16();
			
			Debug.Assert(vertCount == tcCount);
			Debug.Assert(vertCount == normalCount);
			
			Br.ReadUInt16();
			var scale = (double) (1UL << Br.ReadUInt16());
			var vertices = Enumerable.Range(0, vertCount)
				.Select(_ => new Vec3(Br.ReadInt16(), Br.ReadInt16(), Br.ReadInt16()) / scale + center).ToArray();
			var texcoords = Enumerable.Range(0, vertCount)
				.Select(_ => NewFormat ? Br.ReadVec2() : new Vec2(Br.ReadInt16(), Br.ReadInt16()) / 256).ToArray();
			var normals = Enumerable.Range(0, vertCount)
				.Select(_ => new Vec3(Br.ReadSByte(), Br.ReadSByte(), Br.ReadSByte()) / 127).ToArray();
			var colors = Enumerable.Range(0, colorCount)
				.Select(_ => Br.ReadUInt32()).ToArray();
			var polygons = Enumerable.Range(0, polyCount)
				.Select(_ => (Br.ReadUInt16() == 0, (uint) Br.ReadUInt16(), (uint) Br.ReadUInt16(), (uint) Br.ReadUInt16())).ToArray();
			var vertPieces = Enumerable.Range(0, vertPieceCount)
				.Select(_ => (Br.ReadUInt16(), Br.ReadUInt16())).ToArray();
			var polyTex = Enumerable.Range(0, polyTexCount)
				.Select(_ => ((uint) Br.ReadUInt16(), (uint) Br.ReadUInt16())).ToArray();
			
			return new Fragment36 {
				TextureListReference = texRef, 
				Vertices = vertices, 
				Normals = normals, 
				TexCoords = texcoords, 
				Polygons = polygons, 
				PolyTexs = polyTex
			};
		}
		
		Fragment37 Read37() {
			return new Fragment37();
		}
		
		Reference<T> ReadRef<T>() where T : class => Br.ReadRef<T>(this);

		public string GetReferenceName(int reference) => reference > 0 ? Fragments[reference - 1].Name : GetString(-reference);
		public object GetReference(int reference) => reference > 0 ? Fragments[reference - 1].Fragment : GetString(-reference);

		string ReadDecodeString(int size) => Encoding.ASCII.GetString(Br.ReadBytes(size).Select((x, i) => (byte) (x ^ StringHashKey[i % 8])).ToArray());

		string GetString(int pos) => StringHash.Substring(pos).Split('\0', 2)[0];
		string GetString(uint pos) => GetString((int) pos);
	}
}