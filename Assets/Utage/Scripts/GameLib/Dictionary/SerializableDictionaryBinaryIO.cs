// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.IO;

namespace Utage
{

	/// <summary>
	/// シリアライズ可能な自作Dictionary用のキーバリュー
	/// バリナリIO機能つき
	/// </summary>
	[System.Serializable]
	public abstract class SerializableDictionaryBinaryIOKeyValue : SerializableDictionaryKeyValue
	{
		/// <summary>
		/// バイナリ読み込み
		/// </summary>
		/// <param name="reader">バイナリリーダー</param>
		public abstract void Read(BinaryReader reader);

		/// <summary>
		/// バイナリ書き込み
		/// </summary>
		/// <param name="writer">バイナリライター</param>
		public abstract void Write(BinaryWriter writer);
	};

	/// <summary>
	/// シリアライズ可能な自作Dictionary
	/// バリナリIO機能つき
	/// </summary>
	[System.Serializable]
	public abstract class SerializableDictionaryBinaryIO<T> : SerializableDictionary<T>
		where T : SerializableDictionaryBinaryIOKeyValue, new()
	{
		/// <summary>
		/// バイナリ読み込み
		/// </summary>
		/// <param name="reader">バイナリリーダー</param>
		public void Read(BinaryReader reader)
		{
			this.Clear();
			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++)
			{
				T keyValue = new T();
				keyValue.Read(reader);
				Add(keyValue);
			}
		}

		/// <summary>
		/// バイナリ書き込み
		/// </summary>
		/// <param name="writer">バイナリライター</param>
		public void Write(BinaryWriter writer)
		{
			writer.Write(this.Count);
			foreach (SerializableDictionaryBinaryIOKeyValue keyValue in List)
			{
				keyValue.Write(writer);
			}
		}
	};


	//汎用的に使うものを定義しておく

	/// <summary>
	/// boolのDictionary用のキーバリュー
	/// </summary>
	[System.Serializable]
	public class DictionaryKeyValueBool : SerializableDictionaryBinaryIOKeyValue
	{
		/// <summary>値</summary>
		public bool value;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="value">値</param>
		public DictionaryKeyValueBool(string key, bool value)
		{
			Init(key, value);
		}

		/// <summary>
		/// コンストラクタ(Genericを使うために一応定義)
		/// </summary>
		public DictionaryKeyValueBool() { }

		/// <summary>
		/// 初期化
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="value">値</param>
		void Init(string key, bool value)
		{
			InitKey(key);
			this.value = value;
		}

		/// <summary>
		/// バイナリ読み込み
		/// </summary>
		/// <param name="reader">バイナリリーダー</param>
		public override void Read(BinaryReader reader)
		{
			InitKey(reader.ReadString());
			this.value = reader.ReadBoolean();
		}

		/// <summary>
		/// バイナリ書き込み
		/// </summary>
		/// <param name="writer">バイナリライター</param>
		public override void Write(BinaryWriter writer)
		{
			writer.Write(this.Key);
			writer.Write(this.value);
		}
	};

	/// <summary>
	/// boolのDictionary
	/// </summary>
	[System.Serializable]
	public class DictionaryBool : SerializableDictionaryBinaryIO<DictionaryKeyValueBool>
	{
		/// <summary>
		/// 値の追加
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="val">値</param>
		public void Add(string key, bool val)
		{
			this.Add(new DictionaryKeyValueBool(key, val));
		}

		/// <summary>
		/// キーから値の取得
		/// </summary>
		/// <param name="key">キー</param>
		/// <returns>値</returns>
		public bool Get(string key)
		{
			DictionaryKeyValueBool keyValue = this.GetValue(key);
			return keyValue.value;
		}

		/// <summary>
		/// キーから値の取得を試みる
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="val">値</param>
		/// <returns>成否</returns>
		public bool TryGetValue(string key, out bool val)
		{
			DictionaryKeyValueBool keyValue;
			if (this.TryGetValue(key, out keyValue))
			{
				val = keyValue.value;
				return true;
			}
			else
			{
				val = false;
				return false;
			}
		}
	}

	/// <summary>
	/// intのDictionary用のキーバリュー
	/// </summary>
	[System.Serializable]
	public class DictionaryKeyValueInt : SerializableDictionaryBinaryIOKeyValue
	{
		/// <summary>値</summary>
		public int value;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="value">値</param>
		public DictionaryKeyValueInt(string key, int value)
		{
			Init(key, value);
		}

		/// <summary>
		/// コンストラクタ(Genericを使うために一応定義)
		/// </summary>
		public DictionaryKeyValueInt() {}

		/// <summary>
		/// 初期化
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="value">値</param>
		void Init(string key, int value)
		{
			InitKey(key);
			this.value = value;
		}

		/// <summary>
		/// バイナリ読み込み
		/// </summary>
		/// <param name="reader">バイナリリーダー</param>
		public override void Read(BinaryReader reader)
		{
			InitKey(reader.ReadString());
			this.value = reader.ReadInt32();
		}

		/// <summary>
		/// バイナリ書き込み
		/// </summary>
		/// <param name="writer">バイナリライター</param>
		public override void Write(BinaryWriter writer)
		{
			writer.Write(this.Key);
			writer.Write(this.value);
		}
	};

	/// <summary>
	/// intのDictionary
	/// </summary>
	[System.Serializable]
	public class DictionaryInt : SerializableDictionaryBinaryIO<DictionaryKeyValueInt>
	{
		/// <summary>
		/// 値の追加
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="val">値</param>
		public void Add(string key, int val)
		{
			this.Add(new DictionaryKeyValueInt(key, val));
		}

		/// <summary>
		/// キーから値の取得
		/// </summary>
		/// <param name="key">キー</param>
		/// <returns>値</returns>
		public int Get(string key)
		{
			DictionaryKeyValueInt keyValue = this.GetValue(key);
			return keyValue.value;
		}

		/// <summary>
		/// キーから値の取得を試みる
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="val">値</param>
		/// <returns>成否</returns>
		public bool TryGetValue(string key, out int val)
		{
			DictionaryKeyValueInt keyValue;
			if (this.TryGetValue(key, out keyValue))
			{
				val = keyValue.value;
				return true;
			}
			else
			{
				val = 0;
				return false;
			}
		}
	}

	/// <summary>
	/// floatのDictionary用のキーバリュー
	/// </summary>
	[System.Serializable]
	public class DictionaryKeyValueFloat : SerializableDictionaryBinaryIOKeyValue
	{
		/// <summary>値</summary>
		public float value;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="value">値</param>
		public DictionaryKeyValueFloat(string key, float value)
		{
			Init(key, value);
		}

		/// <summary>
		/// コンストラクタ(Genericを使うために一応定義)
		/// </summary>
		public DictionaryKeyValueFloat() { }

		/// <summary>
		/// 初期化
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="value">値</param>
		void Init(string key, float value)
		{
			InitKey(key);
			this.value = value;
		}

		/// <summary>
		/// バイナリ読み込み
		/// </summary>
		/// <param name="reader">バイナリリーダー</param>
		public override void Read(BinaryReader reader)
		{
			InitKey(reader.ReadString());
			this.value = reader.ReadSingle();
		}

		/// <summary>
		/// バイナリ書き込み
		/// </summary>
		/// <param name="writer">バイナリライター</param>
		public override void Write(BinaryWriter writer)
		{
			writer.Write(this.Key);
			writer.Write(this.value);
		}
	};

	/// <summary>
	/// floatのDictionary
	/// </summary>
	[System.Serializable]
	public class DictionaryFloat : SerializableDictionaryBinaryIO<DictionaryKeyValueFloat>
	{

		/// <summary>
		/// 値の追加
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="val">値</param>
		public void Add(string key, float val)
		{
			this.Add(new DictionaryKeyValueFloat(key, val));
		}

		/// <summary>
		/// キーから値の取得
		/// </summary>
		/// <param name="key">キー</param>
		/// <returns>値</returns>
		public float Get(string key)
		{
			DictionaryKeyValueFloat keyValue = this.GetValue(key);
			return keyValue.value;
		}

		/// <summary>
		/// キーから値の取得を試みる
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="val">値</param>
		/// <returns>成否</returns>
		public bool TryGetValue(string key, out float val)
		{
			DictionaryKeyValueFloat keyValue;
			if (this.TryGetValue(key, out keyValue))
			{
				val = keyValue.value;
				return true;
			}
			else
			{
				val = 0;
				return false;
			}
		}
	}

	/// <summary>
	/// doubleのDictionary用のキーバリュー
	/// </summary>
	[System.Serializable]
	public class DictionaryKeyValueDouble : SerializableDictionaryBinaryIOKeyValue
	{
		/// <summary>値</summary>
		public double value;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="value">値</param>
		public DictionaryKeyValueDouble(string key, double value)
		{
			Init(key, value);
		}

		/// <summary>
		/// コンストラクタ(Genericを使うために一応定義)
		/// </summary>
		public DictionaryKeyValueDouble() { }

		/// <summary>
		/// 初期化
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="value">値</param>
		void Init(string key, double value)
		{
			InitKey(key);
			this.value = value;
		}

		/// <summary>
		/// バイナリ読み込み
		/// </summary>
		/// <param name="reader">バイナリリーダー</param>
		public override void Read(BinaryReader reader)
		{
			InitKey(reader.ReadString());
			this.value = reader.ReadDouble();
		}

		/// <summary>
		/// バイナリ書き込み
		/// </summary>
		/// <param name="writer">バイナリライター</param>
		public override void Write(BinaryWriter writer)
		{
			writer.Write(this.Key);
			writer.Write(this.value);
		}
	};

	/// <summary>
	/// doubleのDictionary
	/// </summary>
	[System.Serializable]
	public class DictionaryDouble : SerializableDictionaryBinaryIO<DictionaryKeyValueDouble>
	{

		/// <summary>
		/// 値の追加
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="val">値</param>
		public void Add(string key, double val)
		{
			this.Add(new DictionaryKeyValueDouble(key, val));
		}

		/// <summary>
		/// キーから値の取得
		/// </summary>
		/// <param name="key">キー</param>
		/// <returns>値</returns>
		public double Get(string key)
		{
			DictionaryKeyValueDouble keyValue = this.GetValue(key);
			return keyValue.value;
		}

		/// <summary>
		/// キーから値の取得を試みる
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="val">値</param>
		/// <returns>成否</returns>
		public bool TryGetValue(string key, out double val)
		{
			DictionaryKeyValueDouble keyValue;
			if (this.TryGetValue(key, out keyValue))
			{
				val = keyValue.value;
				return true;
			}
			else
			{
				val = 0;
				return false;
			}
		}
	}


	/// <summary>
	/// stringのDictionary用のキーバリュー
	/// </summary>
	[System.Serializable]
	public class DictionaryKeyValueString : SerializableDictionaryBinaryIOKeyValue
	{
		/// <summary>値</summary>
		public string value;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="value">値</param>
		public DictionaryKeyValueString(string key, string value)
		{
			Init(key, value);
		}

		/// <summary>
		/// コンストラクタ(Genericを使うために一応定義)
		/// </summary>
		public DictionaryKeyValueString() { }

		/// <summary>
		/// 初期化
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="value">値</param>
		void Init(string key, string value)
		{
			InitKey(key);
			this.value = value;
		}

		/// <summary>
		/// バイナリ読み込み
		/// </summary>
		/// <param name="reader">バイナリリーダー</param>
		public override void Read(BinaryReader reader)
		{
			InitKey(reader.ReadString());
			this.value = reader.ReadString();
		}

		/// <summary>
		/// バイナリ書き込み
		/// </summary>
		/// <param name="writer">バイナリライター</param>
		public override void Write(BinaryWriter writer)
		{
			writer.Write(this.Key);
			writer.Write(this.value);
		}
	};

	/// <summary>
	/// stringのDictionary
	/// </summary>
	[System.Serializable]
	public class DictionaryString : SerializableDictionaryBinaryIO<DictionaryKeyValueString>
	{

		/// <summary>
		/// 値の追加
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="val">値</param>
		public void Add(string key, string val)
		{
			this.Add(new DictionaryKeyValueString(key, val));
		}

		/// <summary>
		/// キーから値の取得
		/// </summary>
		/// <param name="key">キー</param>
		/// <returns>値</returns>
		public string Get(string key)
		{
			DictionaryKeyValueString keyValue = this.GetValue(key);
			return keyValue.value;
		}

		/// <summary>
		/// キーから値の取得を試みる
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="val">値</param>
		/// <returns>成否</returns>
		public bool TryGetValue(string key, out string val)
		{
			DictionaryKeyValueString keyValue;
			if (this.TryGetValue(key, out keyValue))
			{
				val = keyValue.value;
				return true;
			}
			else
			{
				val = "";
				return false;
			}
		}
	}
}