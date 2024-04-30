namespace NitroSharp.NsScript.VM;

public sealed class SystemVariableLookup
{
    private readonly NsScriptVM _vm;
    private readonly GlobalsLookupTable _nameLookup;

    public readonly int PresentProcess;
    private readonly int _presentPreprocess;
    private readonly int _presentText;

    private readonly int _rButtonDown;
    private readonly int _x360ButtonStartDown;
    private readonly int _x360ButtonADown;
    private readonly int _x360ButtonBDown;
    private readonly int _x360ButtonYDown;

    private readonly int _x360ButtonLeftDown;
    private readonly int _x360ButtonUpDown;
    private readonly int _x360ButtonRightDown;
    private readonly int _x360ButtonDownDown;

    private readonly int _x360ButtonLbDown;
    private readonly int _x360ButtonRbDown;
    private readonly int _backlogEnable;
    private readonly int _backlogRowMax;
    private readonly int _backlogPositionX;
    private readonly int _backlogPositionY;
    private readonly int _backlogRowInterval;
    private readonly int _backlogCharacterWidth;

    private readonly int _positionXTextIcon;
    private readonly int _positionYTextIcon;
    private readonly int _savePath;

    private readonly int _lastText;
    private readonly int _skip;
    private readonly int _textAuto;
    private readonly int _textAutoLock;
    private readonly int _menuLock;
    private readonly int _skipLock;
    private readonly int _backlogLock;

    private ConstantValue _missingVariable = ConstantValue.Null;

    public SystemVariableLookup(NsScriptVM vm)
    {
        _vm = vm;
        _nameLookup = vm.GlobalsLookup;

        _savePath = Lookup("SYSTEM_save_path");

        _presentPreprocess = Lookup("SYSTEM_present_preprocess");
        _presentText = Lookup("SYSTEM_present_text");
        PresentProcess = Lookup("SYSTEM_present_process");
        _rButtonDown = Lookup("SYSTEM_r_button_down");
        _x360ButtonStartDown = Lookup("SYSTEM_XBOX360_button_start_down");
        _x360ButtonADown = Lookup("SYSTEM_XBOX360_button_a_down");
        _x360ButtonBDown = Lookup("SYSTEM_XBOX360_button_b_down");
        _x360ButtonYDown = Lookup("SYSTEM_XBOX360_button_y_down");
        _x360ButtonLeftDown = Lookup("SYSTEM_XBOX360_button_left_down");
        _x360ButtonUpDown = Lookup("SYSTEM_XBOX360_button_up_down");
        _x360ButtonRightDown = Lookup("SYSTEM_XBOX360_button_right_down");
        _x360ButtonDownDown = Lookup("SYSTEM_XBOX360_button_down_down");
        _x360ButtonLbDown = Lookup("SYSTEM_XBOX360_button_lb_down");
        _x360ButtonRbDown = Lookup("SYSTEM_XBOX360_button_rb_down");

        _backlogEnable = Lookup("SYSTEM_backlog_enable");
        _backlogRowMax = Lookup("SYSTEM_backlog_row_max");
        _backlogPositionX = Lookup("SYSTEM_backlog_position_x");
        _backlogPositionY = Lookup("SYSTEM_backlog_position_y");
        _backlogRowInterval = Lookup("SYSTEM_backlog_row_interval");
        _backlogCharacterWidth = Lookup("SYSTEM_backlog_character_width");

        _positionXTextIcon = Lookup("SYSTEM_position_x_text_icon");
        _positionYTextIcon = Lookup("SYSTEM_position_y_text_icon");
        _lastText = Lookup("SYSTEM_last_text");
        _skip = Lookup("SYSTEM_skip");
        _textAuto = Lookup("SYSTEM_text_auto");
        _textAutoLock = Lookup("SYSTEM_text_auto_lock");

        _menuLock = Lookup("SYSTEM_menu_lock");
        _skipLock = Lookup("SYSTEM_skip_lock");
        _backlogLock = Lookup("SYSTEM_backlog_lock");
    }

    private int Lookup(string name)
    {
        if (!_nameLookup.TryLookupSystemVariable(name, out int index))
        {
            if (!_nameLookup.TryLookupSystemFlag(name, out index))
            {
                return -1;
            }
        }
        return index;
    }

    public ref ConstantValue CurrentSubroutineName => ref Var(PresentProcess);
    public ref ConstantValue CurrentDialogueBox => ref Var(_presentPreprocess);
    public ref ConstantValue CurrentDialogueBlock => ref Var(_presentText);
    public ref ConstantValue RightButtonDown => ref Var(_rButtonDown);

    public ref ConstantValue X360StartButtonDown => ref Var(_x360ButtonStartDown);
    public ref ConstantValue X360AButtonDown => ref Var(_x360ButtonADown);
    public ref ConstantValue X360BButtonDown => ref Var(_x360ButtonBDown);
    public ref ConstantValue X360YButtonDown => ref Var(_x360ButtonYDown);

    public ref ConstantValue X360LeftButtonDown => ref Var(_x360ButtonLeftDown);
    public ref ConstantValue X360UpButtonDown => ref Var(_x360ButtonUpDown);
    public ref ConstantValue X360RightButtonDown => ref Var(_x360ButtonRightDown);
    public ref ConstantValue X360DownButtonDown => ref Var(_x360ButtonDownDown);

    public ref ConstantValue X360LbButtonDown => ref Var(_x360ButtonLbDown);
    public ref ConstantValue X360RbButtonDown => ref Var(_x360ButtonRbDown);

    private ref ConstantValue Var(int index)
    {
        return ref index >= 0
            ? ref _vm.GetVariable(index)
            : ref _missingVariable;
    }

    private ref ConstantValue Flag(int index)
    {
        return ref index >= 0
            ? ref _vm.GetFlag(index)
            : ref _missingVariable;
    }

    public ref ConstantValue BacklogEnable => ref _vm.GetVariable(_backlogEnable);
    public ref ConstantValue BacklogRowMax => ref _vm.GetVariable(_backlogRowMax);
    public ref ConstantValue BacklogRowInterval => ref _vm.GetVariable(_backlogRowInterval);
    public ref ConstantValue BacklogPositionX => ref _vm.GetVariable(_backlogPositionX);
    public ref ConstantValue BacklogPositionY => ref _vm.GetVariable(_backlogPositionY);
    public ref ConstantValue BacklogCharacterWidth => ref _vm.GetVariable(_backlogCharacterWidth);

    public ref ConstantValue PositionXTextIcon => ref _vm.GetVariable(_positionXTextIcon);
    public ref ConstantValue PositionYTextIcon => ref _vm.GetVariable(_positionYTextIcon);

    public ref ConstantValue SavePath => ref Flag(_savePath);

    public ref ConstantValue LastText => ref Var(_lastText);

    public ref ConstantValue Skip => ref Var(_skip);
    public ref ConstantValue TextAuto => ref Var(_textAuto);
    public ref ConstantValue TextAutoLock => ref Var(_textAutoLock);

    public ref ConstantValue MenuLock => ref Var(_menuLock);
    public ref ConstantValue SkipLock => ref Var(_skipLock);
    public ref ConstantValue BacklogLock => ref Var(_backlogLock);
}
