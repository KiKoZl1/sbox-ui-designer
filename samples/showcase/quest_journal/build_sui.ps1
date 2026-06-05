# Helper script to author quest_journal.sui as valid V3 JSON.
# Builds an ordered structure using PSCustomObjects, then ConvertTo-Json.
# Single-use builder — kept in-repo for reproducibility but not required at runtime.

$ErrorActionPreference = 'Stop'

function ZeroBox {
    [PSCustomObject]@{
        Left = 0; Top = 0; Right = 0; Bottom = 0
        IsZero = $true; IsUniform = $true
    }
}

function MakeLayout {
    param(
        [int]$X, [int]$Y, [int]$W, [int]$H,
        [string]$Anchor = 'TopLeft',
        [double]$PivotX = 0, [double]$PivotY = 0,
        [int]$ZIndex = 0,
        [string]$JustifyContent = 'FlexStart',
        [string]$AlignItems = 'Stretch'
    )
    [PSCustomObject]@{
        Mode = 'Absolute'
        X = $X; Y = $Y; Width = $W; Height = $H
        MinWidth = $null; MinHeight = $null; MaxWidth = $null; MaxHeight = $null
        Anchor = $Anchor; PivotX = $PivotX; PivotY = $PivotY
        ZIndex = $ZIndex
        FlexDirection = 'Row'
        JustifyContent = $JustifyContent
        AlignItems = $AlignItems
        FlexWrap = 'NoWrap'
        Gap = 0
        Margin = (ZeroBox)
        Padding = (ZeroBox)
    }
}

function MakeStyle {
    param(
        [string]$ClassName = '',
        $BackgroundColor = $null,
        $BorderColor = $null,
        [int]$BorderWidth = 0,
        [int]$BorderRadius = 0,
        [string]$PointerEvents = 'None',
        [string]$Overflow = 'Visible',
        [double]$Opacity = 1.0
    )
    [PSCustomObject]@{
        ClassName = $ClassName
        CustomClasses = @()
        BackgroundColor = $BackgroundColor
        BorderColor = $BorderColor
        BorderWidth = $BorderWidth
        BorderRadius = $BorderRadius
        BackgroundImage = ''
        BackgroundSize = 'Contain'
        BackgroundWidth = 0
        BackgroundHeight = 0
        Opacity = $Opacity
        Visibility = 'Visible'
        PointerEvents = $PointerEvents
        Overflow = $Overflow
    }
}

function MakeProps {
    param([hashtable]$Overrides = @{})

    $defaults = [ordered]@{
        Text = ''
        FontSize = 16
        FontFamily = $null
        FontWeight = 'Normal'
        Color = '#ffffff'
        TextAlign = 'Left'
        LineHeight = $null
        LetterSpacing = 0
        TextOverflow = 'Clip'
        TextSizeMode = 'Auto'
        VerticalAlign = 'Top'
        ImagePath = $null
        Tint = '#ffffff'
        FitMode = 'Contain'
        BackgroundPosition = 'Center'
        Columns = 1
        Rows = 1
        CellWidth = 64
        CellHeight = 64
        GridGap = 4
        AutoFill = $false
        GridStrategy = 'WrappedFlex'
        ButtonText = ''
        ButtonShape = 'Rectangle'
        HoverStyle = $null
        PressedStyle = $null
        DisabledStyle = $null
        FocusedStyle = $null
        IsDisabled = $false
        TransitionEnabled = $true
        TransitionDuration = 0.15
        HoverSound = ''
        PressSound = ''
        Cursor = 'Default'
        ProgressMin = 0
        ProgressMax = 100
        ProgressPreviewValue = 50
        ProgressFillColor = '#4ade80'
        ProgressDirection = 'LeftToRight'
        AutoWrapText = $false
        WrapTextAt = 0
        SlotIndex = 0
        PreviewIconPath = $null
        PreviewCount = 0
        PlaceholderText = ''
        MaxLength = 0
        ReadOnly = $false
        PreviewValue = ''
        SliderMin = 0
        SliderMax = 100
        SliderStep = 1
        SliderOrientation = 'Horizontal'
        SliderTrackColor = '#22222288'
        SliderFillColor = '#4ade80'
        SliderHandleColor = '#ffffff'
        SliderShowValue = $false
        SliderValue = 50
        SliderTooltipBgColor = '#000000'
        SliderTooltipTextColor = '#ffffff'
        ToggleChecked = $false
        ToggleLabelText = ''
        DropDownOptions = @()
        DropDownSelectedIndex = 0
    }

    foreach ($k in $Overrides.Keys) {
        $defaults[$k] = $Overrides[$k]
    }

    [PSCustomObject]$defaults
}

function MakeFlags {
    param([bool]$ExposeAsVariable = $false)
    [PSCustomObject]@{
        Locked = $false
        HiddenInDesigner = $false
        ExposeAsVariable = $ExposeAsVariable
    }
}

function MakeBinding {
    param(
        [string]$Id,
        [string]$Property,
        [string]$VariableId,
        [string]$Mode = 'OneWay',
        [string]$UpdateTrigger = 'OnChange'
    )
    [PSCustomObject]@{
        Id = $Id
        Property = $Property
        Mode = $Mode
        UpdateTrigger = $UpdateTrigger
        Source = [PSCustomObject]@{ VariableId = $VariableId }
        Converters = @()
        FallbackValue = $null
    }
}

function MakeEvents {
    param([hashtable]$Map = @{})
    $obj = [ordered]@{}
    foreach ($k in $Map.Keys) {
        $obj[$k] = [PSCustomObject]@{
            Mode = 'Code'
            Handler = $Map[$k]
            DooPropertyName = $null
            DooBody = $null
        }
    }
    if ($Map.Count -eq 0) {
        return [PSCustomObject]@{}
    }
    [PSCustomObject]$obj
}

function MakeElement {
    param(
        [string]$Id,
        [string]$Name,
        [string]$Type,
        $ParentId,
        [string[]]$Children = @(),
        $Layout,
        $Style,
        $Props,
        $Bindings = @(),
        $Events,
        $Flags = (MakeFlags)
    )

    if ($null -eq $Events) { $Events = (MakeEvents) }

    [PSCustomObject]@{
        Id = $Id
        Name = $Name
        Type = $Type
        ParentId = $ParentId
        Children = $Children
        Flags = $Flags
        Layout = $Layout
        Style = $Style
        Props = $Props
        Bindings = $Bindings
        SuiReference = $null
        Events = $Events
        Notes = $null
        TooltipText = $null
        IsVisible = $true
        ClassOverride = $null
        StyleRef = $null
    }
}

function MakeVariable {
    param(
        [string]$Id,
        [string]$Name,
        [string]$Type,
        $Default,
        [string]$Group,
        [string]$Description
    )
    [PSCustomObject]@{
        Id = $Id
        Name = $Name
        Type = $Type
        Default = $Default
        Source = [PSCustomObject]@{ Kind = 'Manual' }
        Description = $Description
        IsAdvanced = $false
        IsPublic = $true
        Group = $Group
        ResourceType = $null
    }
}

$elements = New-Object System.Collections.Generic.List[object]

# ─── EL #1 root ───
$elements.Add( (MakeElement `
    -Id 'root' -Name 'Root' -Type 'Canvas' -ParentId $null `
    -Children @('el_scrim','el_card') `
    -Layout (MakeLayout -X 0 -Y 0 -W 1920 -H 1080) `
    -Style (MakeStyle -ClassName 'root' -PointerEvents 'None') `
    -Props (MakeProps)
))

# ─── EL #2 el_scrim ───
$elements.Add( (MakeElement `
    -Id 'el_scrim' -Name 'Scrim' -Type 'Panel' -ParentId 'root' `
    -Layout (MakeLayout -X 0 -Y 0 -W 1920 -H 1080) `
    -Style (MakeStyle -ClassName 'scrim' -BackgroundColor '#00000099' -PointerEvents 'All') `
    -Props (MakeProps)
))

# ─── EL #3 el_card ───
$elements.Add( (MakeElement `
    -Id 'el_card' -Name 'Card' -Type 'Panel' -ParentId 'root' `
    -Children @('el_title','el_tab_active','el_tab_completed','el_tab_failed','el_list','el_detail') `
    -Layout (MakeLayout -X 0 -Y 0 -W 1100 -H 700 -Anchor 'MiddleCenter' -PivotX 0.5 -PivotY 0.5 -ZIndex 1) `
    -Style (MakeStyle -ClassName 'card' -BackgroundColor '#1a1a1ddd' -BorderColor '#2a2a2e' -BorderWidth 1 -BorderRadius 12 -PointerEvents 'All') `
    -Props (MakeProps)
))

# ─── EL #4 el_title ───
$elements.Add( (MakeElement `
    -Id 'el_title' -Name 'TitleLabel' -Type 'Text' -ParentId 'el_card' `
    -Layout (MakeLayout -X 32 -Y 24 -W 400 -H 40) `
    -Style (MakeStyle -ClassName 'title' -PointerEvents 'None') `
    -Props (MakeProps @{
        Text = 'Quest Journal'
        FontSize = 28
        FontWeight = 'Bold'
        Color = '#ffffff'
        TextAlign = 'Left'
        TextSizeMode = 'Fixed'
        VerticalAlign = 'Center'
    })
))

# ─── EL #5 el_tab_active ───
$elements.Add( (MakeElement `
    -Id 'el_tab_active' -Name 'TabActive' -Type 'Button' -ParentId 'el_card' `
    -Layout (MakeLayout -X 32 -Y 80 -W 156 -H 36 -JustifyContent 'Center' -AlignItems 'Center') `
    -Style (MakeStyle -ClassName 'tab tab-active' -BackgroundColor '#4ade80' -BorderColor '#3a3a3e' -BorderWidth 1 -BorderRadius 6 -PointerEvents 'All') `
    -Props (MakeProps @{
        FontSize = 14
        FontWeight = 'Bold'
        Color = '#0d0d0f'
        TextAlign = 'Center'
        TextSizeMode = 'Fixed'
        VerticalAlign = 'Center'
        ButtonText = 'Active'
        ButtonShape = 'Rectangle'
        Cursor = 'Pointer'
    }) `
    -Events (MakeEvents @{ OnClick = 'OnTabActiveClick' }) `
    -Flags (MakeFlags -ExposeAsVariable $true)
))

# ─── EL #6 el_tab_completed ───
$elements.Add( (MakeElement `
    -Id 'el_tab_completed' -Name 'TabCompleted' -Type 'Button' -ParentId 'el_card' `
    -Layout (MakeLayout -X 200 -Y 80 -W 156 -H 36 -JustifyContent 'Center' -AlignItems 'Center') `
    -Style (MakeStyle -ClassName 'tab' -BackgroundColor '#1f2937' -BorderColor '#2a2a2e' -BorderWidth 1 -BorderRadius 6 -PointerEvents 'All') `
    -Props (MakeProps @{
        FontSize = 14
        FontWeight = 'Normal'
        Color = '#9ca3af'
        TextAlign = 'Center'
        TextSizeMode = 'Fixed'
        VerticalAlign = 'Center'
        ButtonText = 'Completed'
        ButtonShape = 'Rectangle'
        Cursor = 'Pointer'
    }) `
    -Events (MakeEvents @{ OnClick = 'OnTabCompletedClick' }) `
    -Flags (MakeFlags -ExposeAsVariable $true)
))

# ─── EL #7 el_tab_failed ───
$elements.Add( (MakeElement `
    -Id 'el_tab_failed' -Name 'TabFailed' -Type 'Button' -ParentId 'el_card' `
    -Layout (MakeLayout -X 368 -Y 80 -W 156 -H 36 -JustifyContent 'Center' -AlignItems 'Center') `
    -Style (MakeStyle -ClassName 'tab' -BackgroundColor '#1f2937' -BorderColor '#2a2a2e' -BorderWidth 1 -BorderRadius 6 -PointerEvents 'All') `
    -Props (MakeProps @{
        FontSize = 14
        FontWeight = 'Normal'
        Color = '#9ca3af'
        TextAlign = 'Center'
        TextSizeMode = 'Fixed'
        VerticalAlign = 'Center'
        ButtonText = 'Failed'
        ButtonShape = 'Rectangle'
        Cursor = 'Pointer'
    }) `
    -Events (MakeEvents @{ OnClick = 'OnTabFailedClick' }) `
    -Flags (MakeFlags -ExposeAsVariable $true)
))

# ─── EL #8 el_list ───
$elements.Add( (MakeElement `
    -Id 'el_list' -Name 'QuestList' -Type 'Panel' -ParentId 'el_card' `
    -Children @('el_quest_card_1','el_quest_card_2','el_quest_card_3') `
    -Layout (MakeLayout -X 32 -Y 140 -W 360 -H 500) `
    -Style (MakeStyle -ClassName 'quest-list' -BackgroundColor '#0d0d0fcc' -BorderRadius 8 -PointerEvents 'All') `
    -Props (MakeProps)
))

# ─── EL #9 el_quest_card_1 ───
$elements.Add( (MakeElement `
    -Id 'el_quest_card_1' -Name 'QuestCard1' -Type 'Button' -ParentId 'el_list' `
    -Layout (MakeLayout -X 8 -Y 8 -W 344 -H 64 -JustifyContent 'Center' -AlignItems 'Center') `
    -Style (MakeStyle -ClassName 'quest-card quest-card-selected' -BackgroundColor '#374151' -BorderColor '#3a3a3e' -BorderWidth 1 -BorderRadius 6 -PointerEvents 'All') `
    -Props (MakeProps @{
        FontSize = 14
        FontWeight = 'Bold'
        Color = '#ffffff'
        TextAlign = 'Center'
        TextSizeMode = 'Fixed'
        VerticalAlign = 'Center'
        ButtonText = 'Slay 10 Zombies'
        ButtonShape = 'Rectangle'
        Cursor = 'Pointer'
    }) `
    -Events (MakeEvents @{ OnClick = 'OnQuest1Click' }) `
    -Flags (MakeFlags -ExposeAsVariable $true)
))

# ─── EL #10 el_quest_card_2 ───
$elements.Add( (MakeElement `
    -Id 'el_quest_card_2' -Name 'QuestCard2' -Type 'Button' -ParentId 'el_list' `
    -Layout (MakeLayout -X 8 -Y 80 -W 344 -H 64 -JustifyContent 'Center' -AlignItems 'Center') `
    -Style (MakeStyle -ClassName 'quest-card' -BackgroundColor '#1f2937' -BorderColor '#2a2a2e' -BorderWidth 1 -BorderRadius 6 -PointerEvents 'All') `
    -Props (MakeProps @{
        FontSize = 14
        FontWeight = 'Normal'
        Color = '#e5e7eb'
        TextAlign = 'Center'
        TextSizeMode = 'Fixed'
        VerticalAlign = 'Center'
        ButtonText = 'Collect 5 Herbs'
        ButtonShape = 'Rectangle'
        Cursor = 'Pointer'
    }) `
    -Events (MakeEvents @{ OnClick = 'OnQuest2Click' }) `
    -Flags (MakeFlags -ExposeAsVariable $true)
))

# ─── EL #11 el_quest_card_3 ───
$elements.Add( (MakeElement `
    -Id 'el_quest_card_3' -Name 'QuestCard3' -Type 'Button' -ParentId 'el_list' `
    -Layout (MakeLayout -X 8 -Y 152 -W 344 -H 64 -JustifyContent 'Center' -AlignItems 'Center') `
    -Style (MakeStyle -ClassName 'quest-card' -BackgroundColor '#1f2937' -BorderColor '#2a2a2e' -BorderWidth 1 -BorderRadius 6 -PointerEvents 'All') `
    -Props (MakeProps @{
        FontSize = 14
        FontWeight = 'Normal'
        Color = '#e5e7eb'
        TextAlign = 'Center'
        TextSizeMode = 'Fixed'
        VerticalAlign = 'Center'
        ButtonText = 'Talk to Mayor'
        ButtonShape = 'Rectangle'
        Cursor = 'Pointer'
    }) `
    -Events (MakeEvents @{ OnClick = 'OnQuest3Click' }) `
    -Flags (MakeFlags -ExposeAsVariable $true)
))

# ─── EL #12 el_detail ───
$elements.Add( (MakeElement `
    -Id 'el_detail' -Name 'QuestDetail' -Type 'Panel' -ParentId 'el_card' `
    -Children @('el_quest_title','el_quest_description','el_objectives_header','el_obj1_text','el_obj1_bar','el_obj2_text','el_obj2_bar','el_obj3_text','el_obj3_bar','el_rewards_header','el_reward_text') `
    -Layout (MakeLayout -X 412 -Y 140 -W 656 -H 500) `
    -Style (MakeStyle -ClassName 'quest-detail' -BackgroundColor '#0d0d0fcc' -BorderRadius 8 -PointerEvents 'All') `
    -Props (MakeProps)
))

# ─── EL #13 el_quest_title ───
$elements.Add( (MakeElement `
    -Id 'el_quest_title' -Name 'QuestTitleLabel' -Type 'Text' -ParentId 'el_detail' `
    -Layout (MakeLayout -X 16 -Y 16 -W 600 -H 32) `
    -Style (MakeStyle -ClassName 'quest-detail-title' -PointerEvents 'None') `
    -Props (MakeProps @{
        Text = 'Slay 10 Zombies'
        FontSize = 24
        FontWeight = 'Bold'
        Color = '#ffffff'
        TextAlign = 'Left'
        TextSizeMode = 'Fixed'
        VerticalAlign = 'Center'
    }) `
    -Bindings @( (MakeBinding -Id 'bind_quest_title' -Property 'Text' -VariableId 'var_selected_quest_title') )
))

# ─── EL #14 el_quest_description ───
$elements.Add( (MakeElement `
    -Id 'el_quest_description' -Name 'QuestDescriptionLabel' -Type 'Text' -ParentId 'el_detail' `
    -Layout (MakeLayout -X 16 -Y 56 -W 600 -H 80) `
    -Style (MakeStyle -ClassName 'quest-detail-desc' -PointerEvents 'None') `
    -Props (MakeProps @{
        Text = 'Reports of zombie sightings near the village graveyard. Clear them out.'
        FontSize = 14
        FontWeight = 'Normal'
        Color = '#9ca3af'
        TextAlign = 'Left'
        TextSizeMode = 'Fixed'
        VerticalAlign = 'Top'
        AutoWrapText = $true
        WrapTextAt = 600
    }) `
    -Bindings @( (MakeBinding -Id 'bind_quest_desc' -Property 'Text' -VariableId 'var_selected_quest_description') )
))

# ─── EL #15 el_objectives_header ───
$elements.Add( (MakeElement `
    -Id 'el_objectives_header' -Name 'ObjectivesHeader' -Type 'Text' -ParentId 'el_detail' `
    -Layout (MakeLayout -X 16 -Y 156 -W 200 -H 20) `
    -Style (MakeStyle -ClassName 'section-header' -PointerEvents 'None') `
    -Props (MakeProps @{
        Text = 'Objectives'
        FontSize = 14
        FontWeight = 'Bold'
        Color = '#9ca3af'
        TextAlign = 'Left'
        TextSizeMode = 'Fixed'
        VerticalAlign = 'Center'
        LetterSpacing = 1
    })
))

# ─── EL #16 el_obj1_text ───
$elements.Add( (MakeElement `
    -Id 'el_obj1_text' -Name 'Obj1Text' -Type 'Text' -ParentId 'el_detail' `
    -Layout (MakeLayout -X 16 -Y 184 -W 400 -H 24) `
    -Style (MakeStyle -ClassName 'objective-text' -PointerEvents 'None') `
    -Props (MakeProps @{
        Text = 'Slay zombies (3/10)'
        FontSize = 14
        FontWeight = 'Normal'
        Color = '#e5e7eb'
        TextAlign = 'Left'
        TextSizeMode = 'Fixed'
        VerticalAlign = 'Center'
    }) `
    -Bindings @( (MakeBinding -Id 'bind_obj1_text' -Property 'Text' -VariableId 'var_objective_1_text') )
))

# ─── EL #17 el_obj1_bar ───
$elements.Add( (MakeElement `
    -Id 'el_obj1_bar' -Name 'Obj1Bar' -Type 'ProgressBar' -ParentId 'el_detail' `
    -Layout (MakeLayout -X 440 -Y 190 -W 180 -H 12) `
    -Style (MakeStyle -ClassName 'objective-bar' -BackgroundColor '#1f2937' -BorderRadius 6 -PointerEvents 'None' -Overflow 'Hidden') `
    -Props (MakeProps @{
        ProgressMin = 0
        ProgressMax = 1
        ProgressPreviewValue = 0.3
        ProgressFillColor = '#4ade80'
        ProgressDirection = 'LeftToRight'
    }) `
    -Bindings @( (MakeBinding -Id 'bind_obj1_value' -Property 'Value' -VariableId 'var_objective_1_progress') )
))

# ─── EL #18 el_obj2_text ───
$elements.Add( (MakeElement `
    -Id 'el_obj2_text' -Name 'Obj2Text' -Type 'Text' -ParentId 'el_detail' `
    -Layout (MakeLayout -X 16 -Y 224 -W 400 -H 24) `
    -Style (MakeStyle -ClassName 'objective-text' -PointerEvents 'None') `
    -Props (MakeProps @{
        Text = 'Return to Mayor'
        FontSize = 14
        FontWeight = 'Normal'
        Color = '#e5e7eb'
        TextAlign = 'Left'
        TextSizeMode = 'Fixed'
        VerticalAlign = 'Center'
    }) `
    -Bindings @( (MakeBinding -Id 'bind_obj2_text' -Property 'Text' -VariableId 'var_objective_2_text') )
))

# ─── EL #19 el_obj2_bar ───
$elements.Add( (MakeElement `
    -Id 'el_obj2_bar' -Name 'Obj2Bar' -Type 'ProgressBar' -ParentId 'el_detail' `
    -Layout (MakeLayout -X 440 -Y 230 -W 180 -H 12) `
    -Style (MakeStyle -ClassName 'objective-bar' -BackgroundColor '#1f2937' -BorderRadius 6 -PointerEvents 'None' -Overflow 'Hidden') `
    -Props (MakeProps @{
        ProgressMin = 0
        ProgressMax = 1
        ProgressPreviewValue = 0.0
        ProgressFillColor = '#4ade80'
        ProgressDirection = 'LeftToRight'
    }) `
    -Bindings @( (MakeBinding -Id 'bind_obj2_value' -Property 'Value' -VariableId 'var_objective_2_progress') )
))

# ─── EL #20 el_obj3_text ───
$elements.Add( (MakeElement `
    -Id 'el_obj3_text' -Name 'Obj3Text' -Type 'Text' -ParentId 'el_detail' `
    -Layout (MakeLayout -X 16 -Y 264 -W 400 -H 24) `
    -Style (MakeStyle -ClassName 'objective-text' -PointerEvents 'None') `
    -Props (MakeProps @{
        Text = ''
        FontSize = 14
        FontWeight = 'Normal'
        Color = '#e5e7eb'
        TextAlign = 'Left'
        TextSizeMode = 'Fixed'
        VerticalAlign = 'Center'
    }) `
    -Bindings @( (MakeBinding -Id 'bind_obj3_text' -Property 'Text' -VariableId 'var_objective_3_text') )
))

# ─── EL #21 el_obj3_bar ───
$elements.Add( (MakeElement `
    -Id 'el_obj3_bar' -Name 'Obj3Bar' -Type 'ProgressBar' -ParentId 'el_detail' `
    -Layout (MakeLayout -X 440 -Y 270 -W 180 -H 12) `
    -Style (MakeStyle -ClassName 'objective-bar' -BackgroundColor '#1f2937' -BorderRadius 6 -PointerEvents 'None' -Overflow 'Hidden') `
    -Props (MakeProps @{
        ProgressMin = 0
        ProgressMax = 1
        ProgressPreviewValue = 0.0
        ProgressFillColor = '#4ade80'
        ProgressDirection = 'LeftToRight'
    }) `
    -Bindings @( (MakeBinding -Id 'bind_obj3_value' -Property 'Value' -VariableId 'var_objective_3_progress') )
))

# ─── EL #22 el_rewards_header ───
$elements.Add( (MakeElement `
    -Id 'el_rewards_header' -Name 'RewardsHeader' -Type 'Text' -ParentId 'el_detail' `
    -Layout (MakeLayout -X 16 -Y 320 -W 200 -H 20) `
    -Style (MakeStyle -ClassName 'section-header' -PointerEvents 'None') `
    -Props (MakeProps @{
        Text = 'Rewards'
        FontSize = 14
        FontWeight = 'Bold'
        Color = '#9ca3af'
        TextAlign = 'Left'
        TextSizeMode = 'Fixed'
        VerticalAlign = 'Center'
        LetterSpacing = 1
    })
))

# ─── EL #23 el_reward_text ───
$elements.Add( (MakeElement `
    -Id 'el_reward_text' -Name 'RewardLabel' -Type 'Text' -ParentId 'el_detail' `
    -Layout (MakeLayout -X 16 -Y 348 -W 600 -H 24) `
    -Style (MakeStyle -ClassName 'reward-text' -PointerEvents 'None') `
    -Props (MakeProps @{
        Text = '300 gold + Iron Sword'
        FontSize = 14
        FontWeight = 'Bold'
        Color = '#fbbf24'
        TextAlign = 'Left'
        TextSizeMode = 'Fixed'
        VerticalAlign = 'Center'
    }) `
    -Bindings @( (MakeBinding -Id 'bind_reward_text' -Property 'Text' -VariableId 'var_selected_quest_reward_text') )
))

# ─── Variables ───
$variables = @(
    (MakeVariable -Id 'var_current_tab_text' -Name 'CurrentTabText' -Type 'string' -Default 'Active' -Group 'Tabs' -Description 'Which tab is currently shown — "Active", "Completed", or "Failed". Controller updates on tab-button click; helper for any UI that displays the active tab label.')
    (MakeVariable -Id 'var_selected_quest_title' -Name 'SelectedQuestTitle' -Type 'string' -Default 'Slay 10 Zombies' -Group 'Detail' -Description 'Title of the currently selected quest. OneWay-bound to el_quest_title. Controller writes via PushSelectedQuest() when the user clicks a quest card.')
    (MakeVariable -Id 'var_selected_quest_description' -Name 'SelectedQuestDescription' -Type 'string' -Default 'Reports of zombie sightings near the village graveyard. Clear them out.' -Group 'Detail' -Description 'Long-form description of the currently selected quest. OneWay-bound to el_quest_description.')
    (MakeVariable -Id 'var_objective_1_text' -Name 'Objective1Text' -Type 'string' -Default 'Slay zombies (3/10)' -Group 'Objectives' -Description 'Label of objective #1 on the active quest. Empty string hides the row visually (controller may decide to keep the slot blank).')
    (MakeVariable -Id 'var_objective_1_progress' -Name 'Objective1Progress' -Type 'float' -Default 0.3 -Group 'Objectives' -Description 'Progress 0..1 for objective #1. Bound to el_obj1_bar (ProgressBar Min=0 Max=1).')
    (MakeVariable -Id 'var_objective_2_text' -Name 'Objective2Text' -Type 'string' -Default 'Return to Mayor' -Group 'Objectives' -Description 'Label of objective #2 on the active quest.')
    (MakeVariable -Id 'var_objective_2_progress' -Name 'Objective2Progress' -Type 'float' -Default 0.0 -Group 'Objectives' -Description 'Progress 0..1 for objective #2.')
    (MakeVariable -Id 'var_objective_3_text' -Name 'Objective3Text' -Type 'string' -Default '' -Group 'Objectives' -Description 'Label of objective #3 on the active quest. Empty when the quest has fewer than 3 objectives.')
    (MakeVariable -Id 'var_objective_3_progress' -Name 'Objective3Progress' -Type 'float' -Default 0.0 -Group 'Objectives' -Description 'Progress 0..1 for objective #3.')
    (MakeVariable -Id 'var_selected_quest_reward_text' -Name 'SelectedQuestRewardText' -Type 'string' -Default '300 gold + Iron Sword' -Group 'Rewards' -Description 'Gold + items the player earns on completion. OneWay-bound to el_reward_text.')
)

# ─── Document root ───
$document = [PSCustomObject]@{
    SchemaVersion = 3
    DocumentId = 'sui_showcase_quest_journal_a4f1c2'
    Name = 'quest_journal'
    CreatedWith = 'Sbox UI Designer'
    DesignerVersion = '0.1.0'
    Canvas = [PSCustomObject]@{
        BaseWidth = 1920
        BaseHeight = 1080
        ScaleMode = 'ScreenHeight1080'
        SafeArea = [PSCustomObject]@{
            Enabled = $false
            Left = 0; Top = 0; Right = 0; Bottom = 0
        }
        BackgroundPreview = [PSCustomObject]@{
            Type = 'Color'
            Color = '#101010'
            ImagePath = $null
        }
        PreviewWidth = 0
        PreviewHeight = 0
    }
    Settings = [PSCustomObject]@{
        AutoPreview = $true
        PreviewDebounceMs = 350
        SnapToGrid = $true
        GridSize = 8
        ShowRulers = $true
        ShowAnchors = $false
        ShowSafeArea = $false
        ShowGrid = $false
        ShowAlignmentGuides = $true
        ShowLayoutBounds = $true
        ShowWidgetInfo = $false
        ResponsiveDebug = $false
        CanvasZoom = 1
        CanvasPanX = 0
        CanvasPanY = 0
    }
    Elements = $elements.ToArray()
    Variables = $variables
    PreviewData = $null
    Animations = @()
    Output = [PSCustomObject]@{
        Configured = $true
        RootFolder = 'Code/Samples/QuestJournal'
        Namespace = 'Sandbox.Samples'
        ClassName = 'QuestJournal'
        GenerateRazor = $true
        GenerateScss = $true
        GenerateGeneratedCs = $false
        GenerateUserCsIfMissing = $false
        GenerateCustomScssIfMissing = $false
    }
    Manifest = [PSCustomObject]@{
        GeneratedFiles = @()
    }
}

$root = [PSCustomObject]@{
    Document = $document
    __references = @()
    __version = 0
}

$json = $root | ConvertTo-Json -Depth 50
$out = 'C:/DEV/Surprise/sbox-ui-designer/samples/showcase/quest_journal/quest_journal.sui'
[System.IO.File]::WriteAllText($out, $json, [System.Text.UTF8Encoding]::new($false))
Write-Output "WROTE: $out"
Write-Output "BYTES: $((Get-Item $out).Length)"

# Verify round-trip parses.
try {
    $verify = Get-Content $out -Raw | ConvertFrom-Json
    Write-Output "PARSE: OK ($($verify.Document.Elements.Count) elements, $($verify.Document.Variables.Count) variables)"
} catch {
    Write-Output "PARSE: FAILED - $_"
}
