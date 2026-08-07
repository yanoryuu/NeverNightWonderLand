# Command Reference: UI & External Integration

## Message window operations

For multiple windows, use window names registered in AdvMessageWindowManager. Switching per dialogue line is also possible via the WindowType column.

| Command | Function | Arg1 | Arg2–Arg6 |
|---|---|---|---|
| ShowMessageWindow | Force-show the window (normally it doesn't appear until a text command) | — | — |
| HideMessageWindow | Hide the window. **It is automatically re-shown at the top of the next page that displays text**, so the hidden state only lasts through non-text sections (Wait, effects, etc.) | — | — |
| InitMessageWindow | Initialize the windows in use; specifying multiple shows them simultaneously | Window name | Window name(s) (additional; up to the first blank) |
| ChangeMessageWindow | Switch the active window (specifying an inactive name swaps them) | Window name | — |

## Menu button visibility

| Command | Function |
|---|---|
| HideMenuButton | Hide menu buttons (persists across page breaks) |
| ShowMenuButton | Cancel the hidden state, back to normal behavior |

## GUI operations (GuiActive / GuiPosition / GuiSize / GuiReset)

Target UIs must be registered in advance on the AdvEngine > UI > **AdvGuiManager** component.

| Command | Function | Arg1 | Arg2 | Arg3 |
|---|---|---|---|---|
| GuiActive | Toggle active ON/OFF | GUI name (blank = all registered UIs) | On/Off | — |
| GuiPosition | Move | GUI name | X | Y |
| GuiSize | Resize | GUI name | Width | Height |
| GuiReset | Reset to initial state | GUI name (blank = all UIs) | — | — |

```csv
GuiActive,MiniMap,TRUE
GuiPosition,MiniMap,100,-50
GuiReset,MiniMap
```

## SendMessage (call your own C# from the scenario)

A lightweight extension hook for calling your own C# code from a scenario. Set the receiver object in AdvScenarioPlayer's "SendMessage" field.

| Command | Arg1 | Arg2–Arg6 |
|---|---|---|
| SendMessage | Identifier (required) | Arbitrary string parameters |

Receiver implementation:

```csharp
// On command execution
void OnDoCommand(AdvCommandSendMessage command)
{
    switch (command.Name) // Arg1
    {
        case "MyCommand":
            // use command.Arg2, command.Arg3 ...
            break;
    }
}
// If waiting is needed (called every frame)
void OnWait(AdvCommandSendMessage command)
{
    command.IsWait = true; // the scenario waits while true
}
```

To feed results back into scenario variables use `engine.Param.TrySetParameter("name", value)`. Stored as a parameter, it is automatically covered by saves.

```csv
SendMessage,MyCommand,パラメータ1
```

## SendMessageByName / BroadcastMessageByName (call by object name)

No prior setup: finds a GameObject in the scene by name and sends a message. Works with dynamically created objects.

| Command | Arg1 | Arg2 | Arg3 | Arg4– |
|---|---|---|---|---|
| SendMessageByName | GameObject name | Method name | Any argument | Any argument |
| BroadcastMessageByName | GameObject name (sent to all its children) | Method name | Search scope | Any argument |

BroadcastMessageByName's Arg3 (search scope): `Default` (whole scene, same as blank) / `UtageObject` (Utage display objects only, lighter) / `RenderTexture` (search prefabs under RenderTextures).

Receiver implementation (method name = Arg2):

```csharp
void Test(AdvCommandSendMessageByName command)
{
    string arg3 = command.ParseCellOptional<string>(AdvColumnName.Arg3, "default");
}
// To wait, control command.IsWait true→false
```

Caution: this uses FindObject, so it has runtime cost. Inactive objects are not found. Watch out for duplicate object names. Prefer SendMessage when speed matters.

```csv
SendMessageByName,MyGameObject,OnCustomEvent,パラメータ1
BroadcastMessageByName,ParentObject,OnCustomEvent,UtageObject
```

