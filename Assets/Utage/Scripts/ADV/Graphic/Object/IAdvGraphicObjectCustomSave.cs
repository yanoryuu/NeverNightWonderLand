// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.IO;

namespace Utage
{
	public interface IAdvGraphicObjectCustomSave
	{
		void WriteSaveDataCustom(BinaryWriter writer);
		void ReadSaveDataCustom(BinaryReader reader);
	}
}

