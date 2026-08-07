// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System;

namespace Utage
{
	/// <summary>
	/// フラグ処理（主にSystem.Flagsのため）
	/// </summary>
	public static class FlagsUtil
	{
		//指定したフラグが全てあるかどうか
		public static bool Has<T>(T value, T flags)
			where T : struct
		{
			try
			{
				return (((int)(object)value & (int)(object)flags) == (int)(object)flags);
			}
			catch
			{
				return false;
			}
		}

		//指定したフラグのどれかがあるか
		public static bool HasAny<T>(T value, T flags)
			where T : struct
		{
			try
			{
				return (((int)(object)value & (int)(object)flags) != 0);
			}
			catch
			{
				return false;
			}
		}

		//指定したフラグと同じ値か
		public static bool Is<T>(T value, T flags)
			where T : struct
		{
			try
			{
				return (int)(object)value == (int)(object)flags;
			}
			catch
			{
				return false;
			}
		}

		//指定したフラグを立てる
		public static T Add<T>(T value, T flags)
			where T : struct
		{
			try
			{
				return (T)(object)(((int)(object)value | (int)(object)flags));
            }
            catch(Exception ex)
			{
				throw new ArgumentException(
					string.Format(
                        "Could not add flags type '{0}'.",
                        typeof(T).Name
                        ), ex);
            }
        }

		//指定したフラグを消す
		public static T Remove<T>(T value, T flags)
			where T : struct
		{
            try
			{
				return (T)(object)(((int)(object)value & ~(int)(object)flags));
            }
            catch (Exception ex) {
                throw new ArgumentException(
                    string.Format(
                        "Could not remove flags type '{0}'.",
                        typeof(T).Name
                        ), ex);
            }
        }

		//指定したフラグをオン・オフする
		public static T SetEnable<T>(T value, T flags, bool isEnable)
			where T : struct
		{
			try
			{
				if (isEnable)
				{
					return Add<T>(value, flags);
				}
				else
				{
					return Remove<T>(value, flags);
				}
			}
			catch (Exception ex)
			{
				throw new ArgumentException(
					string.Format(
						"Could not SetEnable flags type '{0}'.",
						typeof(T).Name
						), ex);
			}
		}

	}
}
