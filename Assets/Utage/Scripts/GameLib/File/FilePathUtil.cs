// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.IO;
using System;

namespace Utage
{

	/// <summary>
	/// ファイルパス関係の処理の便利クラス 
	/// </summary>
	public class FilePathUtil
	{
		//デレィクトリの名前だけ（親のパスなどは無視）取得
		public static string GetDirectoryNameOnly(string path)
		{
			return Path.GetFileName(Path.GetDirectoryName(path));
		}

		//ディレクトリのパスを区切り文字等を変換しないで取得
		public static string GetDirectoryPath(string path)
		{
			int index = Mathf.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
			if (index >= 0)
			{
				return path.Substring(0, index);
			}
			else
			{
				if (path.IndexOf('.') >= 0)
				{
					return "";
				}
				else
				{
					return path;
				}
			}
		}

		//パスを整形
		public static string Format(string path)
		{
			path = path.Replace("\\", "/");
			if (!path.Contains("://"))
			{
				path = path.Replace(":/", "://");
			}
			return path;
		}

		//ファイル名を取得
		public static string GetFileName(string path)
		{
			return Path.GetFileName(path);
		}

		//拡張子を除いたファイル名を取得
		public static string GetFileNameWithoutExtension(string path)
		{
			try
			{
				return Path.GetFileNameWithoutExtension(path);
			}
			catch (System.Exception e)
			{
				Debug.LogError(path + "  " + e.Message);
				return "";
			}
		}

		//拡張子を除いたパス名を取得
		public static string GetPathWithoutExtension(string path)
		{
			int index = path.LastIndexOf('.');
			if (index > 0)
			{
				path = path.Substring(0, index);
			}
			return path;
		}

		//拡張子名を取得
		public static string GetExtension(string path)
		{
			return Path.GetExtension(path);
		}

		//拡張子名を変更
		public static string ChangeExtension(string path, string extenstion)
		{
			return Path.ChangeExtension(path, extenstion);
		}

		/// 大文字、小文字を無視して指定の拡張子かチェック
		public static bool CheckExtension(string path, string ext)
		{
			return string.Compare( GetExtension(path), ext, true ) == 0;
		}


		//指定の二重拡張子を除いた拡張子をチェック
		public static bool CheckExtensionWithOutDouble(string path, string ext, string doubleExtension)
		{
			return CheckExtension( GetExtensionWithOutDouble(path, doubleExtension), ext);
		}

		
		//指定の二重拡張子を除いた拡張子を取得
		public static string GetExtensionWithOutDouble(string path, string doubleExtension)
		{
			string extenstion = Path.GetExtension(path);
			if(string.Compare( extenstion,doubleExtension, true) != 0)
			{
				//拡張子が指定の二重拡張子とは違うのでそのまま返す
				return extenstion;
			}
			else
			{
				//拡張子が指定の二重拡張子の場合、二重拡張子を省いたものを返す
				path = path.Substring(0, path.Length - doubleExtension.Length);
				return Path.GetExtension(path);
			}
		}

		//二重拡張子を追加（既にある場合は追加しない）
		public static string AddDoubleExtension(string path, string doubleExtension)
		{
			if (!CheckExtension(path,doubleExtension))
			{
				//二重拡張子を追加
				path += doubleExtension;
			}
			return path;
		}

		//二重拡張子を削除したファイル名を取得
		public static string GetFileNameWithoutDoubleExtension(string path)
		{
			string name = Path.GetFileNameWithoutExtension(path);
			if (name.Contains("."))
			{
				name = Path.GetFileNameWithoutExtension(name);
			}
			return name;
		}

		/// <summary>
		/// パスが絶対URLかどうか（ホスト名やドライブ名がついているか）
		/// </summary>
		/// <param name="path">パス</param>
		/// <returns>絶対パスの場合はtrue。そうでない場合はfalse</returns>
		public static bool IsAbsoluteUri(string path)
		{
			if (string.IsNullOrEmpty(path)) return false;
			if (path.Length <= 1) return false;

			try
			{
				System.Uri uri = new System.Uri(path, System.UriKind.RelativeOrAbsolute);
				return uri.IsAbsoluteUri;
			}
			catch (System.Exception e)
			{
				Debug.LogError(path + ":" + e.Message);
				return false;
			}
		}

		/// <summary>
		/// 二バイト文字を含むURLをエンコード
		/// </summary>
		/// <param name="url">url</param>
		/// <returns>エンコードしたURL</returns>
		public static string EncodeUrl(string url)
		{
			try
			{
				System.Uri uri = new System.Uri(url.Replace('\\', '/'));
				return uri.AbsoluteUri;
			}
			catch (System.Exception e)
			{
				Debug.LogError(url + ":" + e.Message);
				return url;
			}
		}

		//キャッシュクリアのため、タイムスタンプを設定したURLにする
		public static string ToCacheClearUrl(string url)
		{
			if (url.Contains(Application.streamingAssetsPath))
			{
				if (Application.platform != RuntimePlatform.WebGLPlayer)
				{
					//StreamigAssets以下はタイムスタンプがあるとバグる
					return url;
				}
			}

			string tempurl = string.Format(
				"{0}?datetime={1}",
				url,
				WrapperUnityVersion.DateTimeNow().ToFileTime()
				);
			return tempurl;
		}

		//StreamingAssetsPathsをWWWでロードするためのURLに変更
		public static string ToStreamingAssetsPath(string path)
		{
			return AddFileProtocol( Combine(Application.streamingAssetsPath, path) );
		}

		//StreamingAssetsPathsをWWWでロードするためのURLに変更
		public static string AddFileProtocol(string path)
		{
			if (path.Contains("://"))
			{	//既にプロトコルがある
				return path;
			}
			else
			{
				if (path[0] != '/')
				{
					path = '/' + path;
				}
				return "file://" + path;
			}
		}
		
		//パスを結合する
		public static string Combine(params string[] args)
		{
			string path = "";
			foreach( string str in args)
			{
				if(!string.IsNullOrEmpty(str))
				{
					path = Path.Combine(path, str);
				}
			}
			path = path.Replace("\\", "/");
			return path;
		}

		//ディレクトリ名を削除する
		public static string RemoveDirectory(string path, string directoryPath)
		{
			path = Format(path);
			directoryPath = Format(directoryPath);
			string newPath;
			if (!TryRemoveDirectory(path, directoryPath, out newPath))
			{
				Debug.LogError("RemoveDirectoryPath Error" + " [" + path +  "] " + " [" + directoryPath +  "] " );
			}
			return newPath;
		}

		//ディレクトリ名を削除しようとする
		public static bool TryRemoveDirectory(string path, string directoryPath, out string newPath)
		{
			newPath = path;
			if (!path.StartsWith(directoryPath))
			{
				return false;
			}
			else
			{
				int len = directoryPath.Length;
				if (path.Length > len)
				{
					char c = path[len];
					if(c=='/' || c=='\\')
					{
						len++;
					}
				}
				newPath = path.Remove(0, len);
				return true;
			}
		}

		public static bool IsUnderDirectory(string path, string directoryPath)
		{
			path = Format(path);
			directoryPath = Format(directoryPath);
			return path.StartsWith(directoryPath);
		}

		//絶対パスから相対パスを取得
		public static string ToRelativePath(string root, string path)
		{
			System.Uri u1 = new System.Uri(root);
			System.Uri u2 = new System.Uri(path);

			//絶対Uriから相対Uriを取得する
			System.Uri relativeUri = u1.MakeRelativeUri(u2);
			return relativeUri.ToString();
		}
	}
}
