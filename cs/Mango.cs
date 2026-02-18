
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Boxx {
	/// <summary>
	/// Exception for parsing mango.
	/// </summary>
	public sealed class MangoParseException : System.Exception {
		public MangoParseException(string message) : base(message) {
			
		}
	}

	/// <summary>
	/// A mango object.
	/// </summary>
	public class MangoObject : IDictionary<string, Mango> {
		private Dictionary<string, Mango> map;

		public ICollection<string> Keys => map.Keys;

		public ICollection<Mango> Values => map.Values;

		public int Count => map.Count;

		public bool IsReadOnly => false;

		public Mango this[string key] {
			get => map[key];
			set => map[key] = value;
		}

		public MangoObject() {
			map = new Dictionary<string, Mango>();
		}

		public MangoObject(IDictionary<string, Mango> values) {
			map = new Dictionary<string, Mango>(values);
		}

		public void Add(string key, Mango value) {
			map.Add(key, value);
		}

		public void Add(KeyValuePair<string, Mango> item) {
			((IDictionary<string, Mango>)map).Add(item);
		}

		public void Clear() {
			map.Clear();
		}

		public bool Contains(KeyValuePair<string, Mango> item) {
			return ((IDictionary<string, Mango>)map).Contains(item);
		}

		public bool ContainsKey(string key) {
			return map.ContainsKey(key);
		}

		public void CopyTo(KeyValuePair<string, Mango>[] array, int arrayIndex) {
			((IDictionary<string, Mango>)map).CopyTo(array, arrayIndex);
		}

		public IEnumerator<KeyValuePair<string, Mango>> GetEnumerator() {
			return map.GetEnumerator();
		}

		public bool Remove(string key) {
			return map.Remove(key);
		}

		public bool Remove(KeyValuePair<string, Mango> item) {
			return ((IDictionary<string, Mango>)map).Remove(item);
		}

		public bool TryGetValue(string key, out Mango value) {
			return map.TryGetValue(key, out value);
		}

		/// <summary>
		/// Reads a nil value from the object.
		/// </summary>
		public bool TryReadNil(string key) {
			return map.TryGetValue(key, out var m) && m.IsNil;
		}

		/// <summary>
		/// Reads an int value from the object.
		/// </summary>
		public bool TryReadInt(string key, out int number) {
			if (map.TryGetValue(key, out var m) && m.TryReadInt(out number)) {
				return true;
			}

			number = default;
			return false;
		}

		/// <summary>
		/// Reads an int value from the object.
		/// </summary>
		public int ReadInt(string key, int def) {
			return TryReadInt(key, out int v) ? v : def;
		}

		/// <summary>
		/// Reads a float value from the object.
		/// </summary>
		public bool TryReadFloat(string key, out float number) {
			if (map.TryGetValue(key, out var m) && m.TryReadFloat(out number)) {
				return true;
			}

			number = default;
			return false;
		}

		/// <summary>
		/// Reads a float value from the object.
		/// </summary>
		public float ReadFloat(string key, float def) {
			return TryReadFloat(key, out float v) ? v : def;
		}

		/// <summary>
		/// Reads a boolean value from the object.
		/// </summary>
		public bool TryReadBool(string key, out bool boolean) {
			if (map.TryGetValue(key, out var m) && m.TryReadBool(out boolean)) {
				return true;
			}

			boolean = default;
			return false;
		}

		/// <summary>
		/// Reads a boolean value from the object.
		/// </summary>
		public bool ReadBool(string key, bool def) {
			return TryReadBool(key, out bool v) ? v : def;
		}

		/// <summary>
		/// Reads a string value from the object.
		/// </summary>
		public bool TryReadString(string key, out string str) {
			if (map.TryGetValue(key, out var m) && m.TryReadString(out str)) {
				return true;
			}

			str = default;
			return false;
		}

		/// <summary>
		/// Reads a string value from the object.
		/// </summary>
		public string ReadString(string key, string def) {
			return TryReadString(key, out string v) ? v : def;
		}

		/// <summary>
		/// Reads a list value from the object.
		/// </summary>
		public bool TryReadList(string key, out List<Mango> list) {
			if (map.TryGetValue(key, out var m) && m.TryReadList(out list)) {
				return true;
			}

			list = default;
			return false;
		}

		/// <summary>
		/// Reads a list value from the object.
		/// </summary>
		public List<Mango> ReadList(string key, List<Mango> def) {
			return TryReadList(key, out List<Mango> v) ? v : def;
		}

		/// <summary>
		/// Reads a list value from the object.
		/// </summary>
		public List<Mango> ReadListOrEmpty(string key) {
			return TryReadList(key, out List<Mango> v) ? v : new List<Mango>();
		}

		/// <summary>
		/// Reads an object value from the object.
		/// </summary>
		public bool TryReadObject(string key, out MangoObject obj) {
			if (map.TryGetValue(key, out var m) && m.TryReadObject(out obj)) {
				return true;
			}

			obj = default;
			return false;
		}

		/// <summary>
		/// Reads an object value from the object.
		/// </summary>
		public MangoObject ReadObject(string key, MangoObject def) {
			return TryReadObject(key, out MangoObject v) ? v : def;
		}

		/// <summary>
		/// Reads an object value from the object.
		/// </summary>
		public MangoObject ReadObjectOrEmpty(string key) {
			return TryReadObject(key, out MangoObject v) ? v : new MangoObject();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}

	/// <summary>
	/// A mango value.
	/// </summary>
	public readonly struct Mango {
		private readonly object value;

		[System.Flags]
		public enum EncodeFlags {
			None   = 0,
			Pretty = 1,
			Commas = 2,
			Quotes = 4,
			Null   = 8,

			Json = Commas | Quotes | Null
		}

		/// <summary>
		/// True if the value is nil.
		/// </summary>
		public bool IsNil => value == null;

		public Mango(int number) {
			value = number;
		}

		public Mango(float number) {
			value = number;
		}

		public Mango(bool boolean) {
			value = boolean;
		}

		public Mango(string str) {
			value = str;
		}

		public Mango(List<Mango> list) {
			value = list;
		}

		public Mango(MangoObject obj) {
			value = obj;
		}

		/// <summary>
		/// Reads the mango value as an integer.
		/// </summary>
		public bool TryReadInt(out int number) {
			if (value is int num) {
				number = num;
				return true;
			}

			number = default;
			return false;
		}

		/// <summary>
		/// Reads the mango value as an integer.
		/// </summary>
		public int ReadInt(int def) {
			return TryReadInt(out int v) ? v : def;
		}

		/// <summary>
		/// Reads the mango value as a float.
		/// </summary>
		public bool TryReadFloat(out float number) {
			if (value is int num1) {
				number = num1;
				return true;
			}

			if (value is float num2) {
				number = num2;
				return true;
			}

			number = default;
			return false;
		}

		/// <summary>
		/// Reads the mango value as a float.
		/// </summary>
		public float ReadFloat(float def) {
			return TryReadFloat(out float v) ? v : def;
		}

		/// <summary>
		/// Reads the mango value as a boolean.
		/// </summary>
		public bool TryReadBool(out bool boolean) {
			if (value is bool b) {
				boolean = b;
				return true;
			}

			boolean = default;
			return false;
		}

		/// <summary>
		/// Reads the mango value as a boolean.
		/// </summary>
		public bool ReadBool(bool def) {
			return TryReadBool(out bool v) ? v : def;
		}

		/// <summary>
		/// Reads the mango value as a string.
		/// </summary>
		public bool TryReadString(out string str) {
			if (value is string s) {
				str = s;
				return true;
			}

			str = default;
			return false;
		}

		/// <summary>
		/// Reads the mango value as a string.
		/// </summary>
		public string ReadString(string def) {
			return TryReadString(out string v) ? v : def;
		}

		/// <summary>
		/// Reads the mango value as a list.
		/// </summary>
		public bool TryReadList(out List<Mango> list) {
			if (value is List<Mango> l) {
				list = l;
				return true;
			}

			list = default;
			return false;
		}

		/// <summary>
		/// Reads the mango value as a list.
		/// </summary>
		public List<Mango> ReadList(List<Mango> def = null) {
			return TryReadList(out List<Mango> v) ? v : def;
		}

		/// <summary>
		/// Reads the mango value as an integer.
		/// </summary>
		public bool TryReadObject(out MangoObject obj) {
			if (value is MangoObject o) {
				obj = o;
				return true;
			}

			obj = default;
			return false;
		}

		/// <summary>
		/// Reads the mango value as an object.
		/// </summary>
		public MangoObject ReadObject(MangoObject def = null) {
			return TryReadObject(out MangoObject v) ? v : def;
		}

		/// <summary>
		/// A nil value.
		/// </summary>
		public static Mango Nil => new Mango();

		/// <summary>
		/// Encodes mango data.
		/// </summary>
		public static string Encode(Mango mango, EncodeFlags flags = EncodeFlags.None) {
			Encoder encoder = new Encoder {
				builder = new StringBuilder(),
				indent  = 0,
				flags   = flags
			};

			EncodeMango(mango, ref encoder);

			return encoder.builder.ToString();
		}

		/// <summary>
		/// Parses mango data.
		/// </summary>
		public static Mango Parse(string mango) {
			Parser parser = new Parser {
				data = mango,
				pos  = 0
			};
			
			return ParseMango(ref parser);
		}

		public static implicit operator Mango(int number) {
			return new Mango(number);
		}

		public static implicit operator Mango(float number) {
			return new Mango(number);
		}

		public static implicit operator Mango(bool boolean) {
			return new Mango(boolean);
		}

		public static implicit operator Mango(string str) {
			return new Mango(str);
		}

		public static implicit operator Mango(List<Mango> list) {
			return new Mango(list);
		}

		public static implicit operator Mango(MangoObject obj) {
			return new Mango(obj);
		}

		public static bool operator ==(Mango a, Mango b) {
			return Equals(a, b);
		}

		public static bool operator !=(Mango a, Mango b) {
			return !Equals(a, b);
		}

		public override bool Equals(object obj) {
			return (obj is Mango m && Equals(value, m.value)) || (value == null && obj == null);
		}

		public override int GetHashCode() {
			return value?.GetHashCode() ?? 0;
		}

		public override string ToString() {
			return value?.ToString() ?? "nil";
		}

		private static void EncodeMango(Mango mango, ref Encoder encoder) {
			switch (mango.value) {
				case int i: {
					encoder.Append(i.ToString(CultureInfo.InvariantCulture));
					break;
				}

				case float f: {
					encoder.Append(f.ToString(CultureInfo.InvariantCulture));
					break;
				}

				case bool b: {
					encoder.Append(b ? "true" : "false");
					break;
				}

				case string s: {
					EncodeString(s, ref encoder);
					break;
				}

				case List<Mango> list: {
					EncodeList(list, ref encoder);
					break;
				}

				case MangoObject obj: {
					EncodeObj(obj, ref encoder);
					break;
				}

				default: {
					encoder.Append(encoder.HasFlag(EncodeFlags.Null) ? "null" : "nil");
					break;
				}
			}
		}

		private static void EncodeString(string str, ref Encoder encoder) {
			encoder.Append('"');
			encoder.Append(str.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\"", "\\\""));
			encoder.Append('"');
		}

		private static void EncodeList(List<Mango> list, ref Encoder encoder) {
			encoder.Append('[');

			bool pretty = encoder.HasFlag(EncodeFlags.Pretty);
			bool commas = encoder.HasFlag(EncodeFlags.Commas);

			if (pretty) {
				encoder.indent++;

				for (int i = 0; i < list.Count; i++) {
					if (i > 0 && commas) {
						encoder.Append(',');
					}

					encoder.AppendLineIndent();
					EncodeMango(list[i], ref encoder);
				}

				encoder.indent--;
				encoder.AppendLineIndent();
			}
			else {
				for (int i = 0; i < list.Count; i++) {
					if (i > 0) {
						encoder.Append(commas ? ',' : ' ');
					}

					EncodeMango(list[i], ref encoder);
				}
			}

			encoder.Append(']');
		}

		private static void EncodeKey(string key, ref Encoder encoder) {
			if (!encoder.HasFlag(EncodeFlags.Quotes) && Regex.IsMatch(key, @"^[a-zA-Z0-9_]+$") && !Regex.IsMatch(key, @"^[0-9]+$")) {
				encoder.Append(key);
			}
			else {
				EncodeString(key, ref encoder);
			}
		}

		private static void EncodeObj(MangoObject obj, ref Encoder encoder) {
			encoder.Append('{');

			bool pretty = encoder.HasFlag(EncodeFlags.Pretty);
			bool commas = encoder.HasFlag(EncodeFlags.Commas);
			bool quotes = encoder.HasFlag(EncodeFlags.Quotes);

			if (pretty) {
				encoder.indent++;

				bool first = true;

				foreach (var pair in obj) {
					if (!first && commas) {
						encoder.Append(',');
					}

					first = false;

					encoder.AppendLineIndent();
					EncodeKey(pair.Key, ref encoder);
					encoder.Append(": ");
					EncodeMango(pair.Value, ref encoder);
				}

				encoder.indent--;
				encoder.AppendLineIndent();
			}
			else {
				bool first = true;

				foreach (var pair in obj) {
					if (!first) {
						encoder.Append(commas ? ',' : ' ');
					}

					first = false;

					EncodeKey(pair.Key, ref encoder);
					encoder.Append(':');
					EncodeMango(pair.Value, ref encoder);
				}
			}

			encoder.Append('}');
		}

		private static Mango ParseMango(ref Parser parser) {
			parser.ParseWhite();

			if (parser.TryParse(@"\b(null|nil|false|true)\b", out Match key)) {
				string val = key.Groups[1].Value;
				
				if (val == "true")  return new Mango(true);
				if (val == "false") return new Mango(false);
				
				return Nil;
			}

			if (parser.TryParse(@"\-?\b\d*\.\d+(?:[eE][+-]\d+)?\b", out Match fm)) {
				if (float.TryParse(fm.Value, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out float f)) {
					return new Mango(f);
				}

				throw new MangoParseException("Failed to parse number");
			}

			if (parser.TryParse(@"\-?\b\d+\b", out Match im)) {
				if (int.TryParse(im.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i)) {
					return new Mango(i);
				}

				throw new MangoParseException("Failed to parse integer");
			}

			if (TryParseString(ref parser, out string s)) {
				return new Mango(s);
			}

			if (TryParseList(ref parser, out List<Mango> list)) {
				return new Mango(list);
			}

			if (TryParseObj(ref parser, out MangoObject obj)) {
				return new Mango(obj);
			}

			throw new MangoParseException("Failed to parse mango value");
		}

		private static bool TryParseString(ref Parser parser, out string str) {
			if (parser.TryParse(@"""((?:(?:\\)*\""|[^""])*?)""", out Match sm)) {
				str = sm.Groups[1].Value.Replace("\\\\", "\\").Replace("\\t", "\t").Replace("\\r", "\r").Replace("\\n", "\n").Replace("\\\"", "\"");
				return true;
			}

			str = default;
			return false;
		}

		private static bool TryParseList(ref Parser parser, out List<Mango> list) {
			int start = parser.pos;
			parser.ParseWhite();
			
			if (!parser.TryParse("[")) {
				parser.pos = start;
				list = default;
				return false;
			}

			parser.ParseWhite();

			list = new List<Mango>();

			bool hasSep = false;

			while (!parser.TryParse("]")) {
				if (list.Count > 0) {
					hasSep |= parser.TryParse(",");
					hasSep |= parser.ParseWhite();

					if (!hasSep) {
						throw new MangoParseException("Failed to parse mango list");
					}
				}

				Mango mango = ParseMango(ref parser);

				list.Add(mango);

				hasSep = parser.ParseWhite();
			}

			return true;
		}

		private static bool TryParseObj(ref Parser parser, out MangoObject obj) {
			int start = parser.pos;
			parser.ParseWhite();
			
			if (!parser.TryParse("{")) {
				parser.pos = start;
				obj = default;
				return false;
			}

			parser.ParseWhite();

			obj = new MangoObject();

			bool hasSep = false;

			while (!parser.TryParse("}")) {
				if (obj.Count > 0) {
					hasSep |= parser.TryParse(",");
					hasSep |= parser.ParseWhite();

					if (!hasSep) {
						throw new MangoParseException("Failed to parse mango object");
					}
				}

				if (!TryParseKey(ref parser, out string key)) {
					throw new MangoParseException("Faield to parse mango object key");
				}

				parser.ParseWhite();

				if (!parser.TryParse(":")) {
					throw new MangoParseException("Expected ':' after mango object key");
				}

				parser.ParseWhite();

				Mango mango = ParseMango(ref parser);

				obj[key] = mango;

				hasSep = parser.ParseWhite();
			}

			return true;
		}

		private static bool TryParseKey(ref Parser parser, out string key) {
			int pos = parser.pos;

			if (parser.TryParse(@"\b[a-zA-Z0-9_]+\b", out Match m)) {
				key = m.Value;

				if (Regex.IsMatch(key, @"^[0-9]+$")) {
					parser.pos = pos;
					return false;
				}

				return true;
			}

			return TryParseString(ref parser, out key);
		}

		private struct Encoder {
			public StringBuilder builder;
			public int indent;
			public EncodeFlags flags;

			public bool HasFlag(EncodeFlags flag) {
				return (flags & flag) == flag;
			}

			public void AppendLine() {
				builder.AppendLine();
			}

			public void Append(char text) {
				builder.Append(text);
			}

			public void Append(string text) {
				builder.Append(text);
			}

			public void AppendIndent(int indent) {
				builder.Append('\t', indent);
			}

			public void AppendIndent() {
				builder.Append('\t', indent);
			}

			public void AppendLineIndent() {
				AppendLine();
				AppendIndent();
			}
		}

		private struct Parser {
			public string data;
			public int pos;

			public bool ParseWhite() {
				return TryParse(@"[\s\n\r]+", out _);
			}

			public bool TryParse(string pattern, out Match match) {
				match = new Regex(@"\G" + pattern).Match(data, pos);

				if (match.Success) {
					pos += match.Length;
				}

				return match.Success;
			}

			public bool TryParse(string literal) {
				if (pos + literal.Length > data.Length) return false;

				for (int i = 0; i < literal.Length; i++) {
					if (data[pos + i] != literal[i]) return false;
				}

				pos += literal.Length;

				return true;
			}
		}
	}
}
