
// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using Utage;

namespace UtageExtensions
{
	/// <summary>
	/// バイナリへの読み書き用の拡張メソッド
	/// </summary>
	public static class BinaryIOExtensions
	{
		/// <summary>
		/// バイト配列を長さつきで書き込み
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="bytes"></param>
		public static void WriteBuffer(this BinaryWriter writer, byte[] bytes)
		{
			writer.Write(bytes.Length);
			writer.Write(bytes);
		}

		/// <summary>
		/// onWriteバッファを書き込み、先頭にそのバッファサイズも記録する
		/// </summary>
		public static void WriteBuffer(this BinaryWriter writer, Action<BinaryWriter> onWrite)
		{
			long begin = writer.BaseStream.Position;
			writer.BaseStream.Position += 4;
			onWrite(writer);
			long end = writer.BaseStream.Position;
			int size = (int)(end - begin - 4);
			writer.BaseStream.Position = begin;
			writer.Write(size);
			writer.BaseStream.Position = end;
		}

		/// <summary>
		/// バッファ読み込み
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="bytes"></param>
		public static byte[] ReadBuffer(this BinaryReader reader)
		{
			return reader.ReadBytes( reader.ReadInt32() );
		}

		/// <summary>
		/// バッファを読み込みスキップ
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="bytes"></param>
		public static void SkipBuffer(this BinaryReader reader)
		{
			int size = reader.ReadInt32();
			reader.BaseStream.Position += size;
		}

		/// <summary>
		/// バイト配列として書き込まれたバッファを、onReaderで読み込み
		/// </summary>
		public static void ReadBuffer(this BinaryReader reader, Action<BinaryReader> onRead)
		{
			long begin = reader.BaseStream.Position;
			int size = reader.ReadInt32();
			long end = begin + 4 + size;

			bool isFailed = false;
			try
			{
				onRead(reader);
				isFailed = reader.BaseStream.Position != end;
			}
			catch(Exception e)
			{
				Debug.LogException(e);
				isFailed = true;
				
			}
			//読み込みに失敗したら末端のreaderの位置を調整
			if (isFailed)
			{
				Debug.LogError("Read Buffer Size Error");
				reader.BaseStream.Position = end;
			}
		}

		/// <summary>
		/// Jsonをバイナリ書き込み
		/// </summary>
		public static void WriteJson(this BinaryWriter writer, object obj)
		{
			writer.Write( JsonUtility.ToJson(obj) );
		}


		/// <summary>
		/// Jsonをバイナリ読み込み
		/// </summary>
		public static void ReadJson(this BinaryReader reader, object obj)
		{
			JsonUtility.FromJsonOverwrite(reader.ReadString(), obj);
		}


		/// <summary>
		/// Vector2をバイナリ書き込み
		/// </summary>
		/// <param name="vector">書き込むVector値</param>
		/// <param name="writer">バイナリライター</param>
		public static void Write(this BinaryWriter writer, Vector2 vector )
		{
			writer.Write(vector.x);
			writer.Write(vector.y);
		}

		/// <summary>
		/// Vector2をバイナリ読み込み
		/// </summary>
		/// <param name="reader">バイナリリーダー</param>
		/// <returns>読み込んだVector値</returns>
		public static Vector2 ReadVector2(this BinaryReader reader)
		{
			return new Vector2(reader.ReadSingle(),reader.ReadSingle());
		}

		/// <summary>
		/// Vector3をバイナリ書き込み
		/// </summary>
		/// <param name="vector">書き込むVector値</param>
		/// <param name="writer">バイナリライター</param>
		public static void Write(this BinaryWriter writer, Vector3 vector)
		{
			writer.Write(vector.x);
			writer.Write(vector.y);
			writer.Write(vector.z);
		}

		/// <summary>
		/// Vector3をバイナリ読み込み
		/// </summary>
		/// <param name="reader">バイナリリーダー</param>
		/// <returns>読み込んだVector値</returns>
		public static Vector3 ReadVector3(this BinaryReader reader)
		{
			return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
		}

		/// <summary>
		/// Vector4をバイナリ書き込み
		/// </summary>
		/// <param name="vector">書き込むVector値</param>
		/// <param name="writer">バイナリライター</param>
		public static void Write(this BinaryWriter writer, Vector4 vector)
		{
			writer.Write(vector.x);
			writer.Write(vector.y);
			writer.Write(vector.z);
			writer.Write(vector.w);
		}

		/// <summary>
		/// Vector4をバイナリ読み込み
		/// </summary>
		/// <param name="reader">バイナリリーダー</param>
		/// <returns>読み込んだVector値</returns>
		public static Vector4 ReadVector4(this BinaryReader reader)
		{
			return new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
		}

		/// <summary>
		/// Quaternionをバイナリ書き込み
		/// </summary>
		/// <param name="quaternion">書き込むVector値</param>
		/// <param name="writer">バイナリライター</param>
		public static void Write(this BinaryWriter writer, Quaternion quaternion)
		{
			writer.Write(quaternion.x);
			writer.Write(quaternion.y);
			writer.Write(quaternion.z);
			writer.Write(quaternion.w);
		}

		/// <summary>
		/// Quaternionをバイナリ読み込み
		/// </summary>
		/// <param name="reader">バイナリリーダー</param>
		/// <returns>読み込んだQuaternion値</returns>
		public static Quaternion ReadQuaternion(this BinaryReader reader)
		{
			return new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
		}

		/// <summary>
		/// Transformのローカル情報をバイナリ書き込み
		/// </summary>
		/// <param name="transform">書き込むTransform</param>
		/// <param name="writer">バイナリライター</param>
		public static void WriteLocalTransform(this BinaryWriter writer, Transform transform)
		{
			writer.Write(transform.localPosition);
			writer.Write(transform.localEulerAngles);
			writer.Write(transform.localScale);
		}

		/// <summary>
		/// Transformのローカル情報をバイナリ読み込み
		/// </summary>
		/// <param name="transform">読み込むTransform</param>
		/// <param name="reader">バイナリリーダー/param>
		public static void ReadLocalTransform(this BinaryReader reader, Transform transform)
		{
			transform.localPosition = ReadVector3(reader);
			transform.localEulerAngles = ReadVector3(reader);
			transform.localScale = ReadVector3(reader);
		}

		/// <summary>
		/// Colorをバイナリ書き込み
		/// </summary>
		/// <param name="color">書き込むカラー</param>
		/// <param name="writer">バイナリライター</param>
		public static void Write(this BinaryWriter writer, Color color )
		{
			writer.Write(color.r);
			writer.Write(color.g);
			writer.Write(color.b);
			writer.Write(color.a);
		}

		/// <summary>
		/// Colorをバイナリ書き込み読み込み
		/// </summary>
		/// <param name="reader">バイナリリーダー</param>
		/// <returns>読み込んだカラー値</returns>
		public static Color ReadColor(this BinaryReader reader)
		{
			return new Color(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
		}

		/// <summary>
		/// Rectをバイナリ書き込み
		/// </summary>
		/// <param name="rect">書き込むRect</param>
		/// <param name="writer">バイナリライター</param>
		public static void Write(this BinaryWriter writer, Rect rect)
		{
			writer.Write(rect.xMin);
			writer.Write(rect.yMin);
			writer.Write(rect.width);
			writer.Write(rect.height);
		}

		/// <summary>
		/// Rectをバイナリ読み込み
		/// </summary>
		/// <param name="reader">バイナリリーダー</param>
		/// <returns>読み込んだRect値</returns>
		public static Rect ReadRect(this BinaryReader reader)
		{
			return new Rect(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
		}

		/// <summary>
		/// Boundsをバイナリ書き込み
		/// </summary>
		/// <param name="bounds">書き込むBounds</param>
		/// <param name="writer">バイナリライター</param>
		public static void Write(this BinaryWriter writer, Bounds bounds)
		{
			writer.Write(bounds.center);
			writer.Write(bounds.size);
		}

		/// <summary>
		/// Boundsをバイナリ読み込み
		/// </summary>
		/// <param name="reader">バイナリリーダー</param>
		/// <returns>読み込んだRect値</returns>
		public static Bounds ReadBounds(this BinaryReader reader)
		{
			return new Bounds(reader.ReadVector3(), reader.ReadVector3());
		}

		/// <summary>
		/// AnimationCurveをバイナリ書き込み
		/// </summary>
		/// <param name="animationCurve">書き込むAnimationCurve</param>
		/// <param name="writer">バイナリライター</param>
		public static void Write(this BinaryWriter writer, AnimationCurve animationCurve)
		{
			throw new NotImplementedException();
		}


		/// <summary>
		/// RectTransfomの情報をバイナリ書き込み
		/// </summary>
		/// <param name="rectTransform">書き込むRectTransfom</param>
		/// <param name="writer">バイナリライター</param>
		public static void WriteRectTransfom(this BinaryWriter writer, RectTransform rectTransform)
		{
			writer.WriteLocalTransform(rectTransform as Transform);
			writer.Write(rectTransform.anchoredPosition3D);
			writer.Write(rectTransform.anchorMin);
			writer.Write(rectTransform.anchorMax);
			writer.Write(rectTransform.sizeDelta);
			writer.Write(rectTransform.pivot);
		}


		/// <summary>
		/// RectTransfomの情報をバイナリ読み込み
		/// </summary>
		/// <param name="rectTransform">読み込むRectTransfom</param>
		/// <param name="reader">バイナリリーダー/param>
		internal static void ReadRectTransfom(this BinaryReader reader,RectTransform rectTransform)
		{
			reader.ReadLocalTransform(rectTransform);
			rectTransform.anchoredPosition3D = reader.ReadVector3();
			rectTransform.anchorMin = reader.ReadVector2();
			rectTransform.anchorMax = reader.ReadVector2();
			rectTransform.sizeDelta = reader.ReadVector2();
			rectTransform.pivot = reader.ReadVector2();
		}
	}
}