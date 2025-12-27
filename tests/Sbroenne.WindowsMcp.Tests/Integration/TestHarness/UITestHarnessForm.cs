namespace Sbroenne.WindowsMcp.Tests.Integration.TestHarness;

/// <summary>
/// A comprehensive test harness form with various UI controls for UI Automation testing.
/// Includes tabs with different control types: text inputs, buttons, checkboxes, radio buttons,
/// combo boxes, list views, tree views, data grids, sliders, and progress bars.
/// </summary>
public sealed class UITestHarnessForm : Form
{
    private readonly TabControl _tabControl;

    // Form Controls Tab
    private readonly TextBox _usernameInput;
    private readonly TextBox _passwordInput;
    private readonly Button _submitButton;
    private readonly Button _cancelButton;
    private readonly CheckBox _checkBox1;
    private readonly CheckBox _checkBox2;
    private readonly CheckBox _checkBox3;
    private readonly RadioButton _radioSmall;
    private readonly RadioButton _radioMedium;
    private readonly RadioButton _radioLarge;
    private readonly ComboBox _comboBox;
    private readonly TrackBar _slider;
    private readonly ProgressBar _progressBar;

    // List View Tab
    private readonly ListView _listView;

    // Tree View Tab
    private readonly TreeView _treeView;

    // Data Grid Tab
    private readonly DataGridView _dataGrid;

    // Status
    private readonly Label _statusLabel;

    /// <summary>
    /// Gets the number of times the submit button was clicked.
    /// </summary>
    public int SubmitClickCount { get; private set; }

    /// <summary>
    /// Gets the number of times the cancel button was clicked.
    /// </summary>
    public int CancelClickCount { get; private set; }

    /// <summary>
    /// Gets the current text in the username input.
    /// </summary>
    public string UsernameText => _usernameInput.Text;

    /// <summary>
    /// Gets the current text in the password input.
    /// </summary>
    public string PasswordText => _passwordInput.Text;

    /// <summary>
    /// Gets the checkbox states as a tuple (Option1, Option2, Option3).
    /// </summary>
    public (bool Option1, bool Option2, bool Option3) CheckboxStates =>
        (_checkBox1.Checked, _checkBox2.Checked, _checkBox3.Checked);

    /// <summary>
    /// Gets which radio button is selected (Small, Medium, Large).
    /// </summary>
    public string SelectedSize =>
        _radioSmall.Checked ? "Small" :
        _radioMedium.Checked ? "Medium" :
        _radioLarge.Checked ? "Large" : "None";

    /// <summary>
    /// Gets the selected combo box item.
    /// </summary>
    public string? SelectedComboItem => _comboBox.SelectedItem?.ToString();

    /// <summary>
    /// Gets the slider value.
    /// </summary>
    public int SliderValue => _slider.Value;

    /// <summary>
    /// Gets the progress bar value.
    /// </summary>
    public int ProgressValue => _progressBar.Value;

    /// <summary>
    /// Gets the selected list view item.
    /// </summary>
    public string? SelectedListItem =>
        _listView.SelectedItems.Count > 0 ? _listView.SelectedItems[0].Text : null;

    /// <summary>
    /// Gets the selected tree view node.
    /// </summary>
    public string? SelectedTreeNode => _treeView.SelectedNode?.Text;

    public UITestHarnessForm()
    {
        // Form setup
        Text = "MCP Windows UI Test Harness";
        Size = new Size(700, 550);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.White;

        // Status label at top
        _statusLabel = new Label
        {
            Text = "UI Test Harness Ready",
            Location = new Point(10, 10),
            Size = new Size(660, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.DarkGreen,
        };
        Controls.Add(_statusLabel);

        // Tab control
        _tabControl = new TabControl
        {
            Location = new Point(10, 40),
            Size = new Size(660, 460),
            Name = "MainTabControl",
        };
        Controls.Add(_tabControl);

        // Create tabs
        var formControlsTab = new TabPage("Form Controls") { Name = "FormControlsTab" };
        var listViewTab = new TabPage("List View") { Name = "ListViewTab" };
        var treeViewTab = new TabPage("Tree View") { Name = "TreeViewTab" };
        var dataGridTab = new TabPage("Data Grid") { Name = "DataGridTab" };

        _tabControl.TabPages.AddRange([formControlsTab, listViewTab, treeViewTab, dataGridTab]);

        // =====================
        // Form Controls Tab
        // =====================

        // Text inputs group
        var textGroup = new GroupBox
        {
            Text = "Text Inputs",
            Location = new Point(10, 10),
            Size = new Size(300, 100),
            Name = "TextInputsGroup",
        };
        formControlsTab.Controls.Add(textGroup);

        var usernameLabel = new Label { Text = "Username:", Location = new Point(10, 25), Size = new Size(70, 20) };
        textGroup.Controls.Add(usernameLabel);
        _usernameInput = new TextBox
        {
            Location = new Point(85, 22),
            Size = new Size(200, 23),
            Name = "UsernameInput",
        };
        _usernameInput.TextChanged += (_, _) => UpdateStatus($"Username: {_usernameInput.Text}");
        textGroup.Controls.Add(_usernameInput);

        var passwordLabel = new Label { Text = "Password:", Location = new Point(10, 55), Size = new Size(70, 20) };
        textGroup.Controls.Add(passwordLabel);
        _passwordInput = new TextBox
        {
            Location = new Point(85, 52),
            Size = new Size(200, 23),
            Name = "PasswordInput",
            PasswordChar = '●',
        };
        textGroup.Controls.Add(_passwordInput);

        // Buttons group
        var buttonsGroup = new GroupBox
        {
            Text = "Actions",
            Location = new Point(320, 10),
            Size = new Size(310, 100),
            Name = "ActionsGroup",
        };
        formControlsTab.Controls.Add(buttonsGroup);

        _submitButton = new Button
        {
            Text = "Submit",
            Location = new Point(10, 30),
            Size = new Size(90, 35),
            Name = "SubmitButton",
        };
        _submitButton.Click += (_, _) => { SubmitClickCount++; UpdateStatus($"Submit clicked ({SubmitClickCount} times)"); };
        buttonsGroup.Controls.Add(_submitButton);

        _cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(110, 30),
            Size = new Size(90, 35),
            Name = "CancelButton",
        };
        _cancelButton.Click += (_, _) => { CancelClickCount++; UpdateStatus($"Cancel clicked ({CancelClickCount} times)"); };
        buttonsGroup.Controls.Add(_cancelButton);

        var disabledButton = new Button
        {
            Text = "Disabled",
            Location = new Point(210, 30),
            Size = new Size(90, 35),
            Name = "DisabledButton",
            Enabled = false,
        };
        buttonsGroup.Controls.Add(disabledButton);

        // Options group (checkboxes)
        var optionsGroup = new GroupBox
        {
            Text = "Options",
            Location = new Point(10, 120),
            Size = new Size(200, 120),
            Name = "OptionsGroup",
        };
        formControlsTab.Controls.Add(optionsGroup);

        _checkBox1 = new CheckBox
        {
            Text = "Notifications",
            Location = new Point(10, 25),
            Size = new Size(180, 25),
            Name = "NotificationsCheckbox",
            Checked = true,
        };
        _checkBox1.CheckedChanged += (_, _) => UpdateStatus($"Notifications: {_checkBox1.Checked}");
        optionsGroup.Controls.Add(_checkBox1);

        _checkBox2 = new CheckBox
        {
            Text = "Auto-save",
            Location = new Point(10, 55),
            Size = new Size(180, 25),
            Name = "AutosaveCheckbox",
        };
        _checkBox2.CheckedChanged += (_, _) => UpdateStatus($"Auto-save: {_checkBox2.Checked}");
        optionsGroup.Controls.Add(_checkBox2);

        _checkBox3 = new CheckBox
        {
            Text = "Dark Mode",
            Location = new Point(10, 85),
            Size = new Size(180, 25),
            Name = "DarkModeCheckbox",
        };
        _checkBox3.CheckedChanged += (_, _) => UpdateStatus($"Dark Mode: {_checkBox3.Checked}");
        optionsGroup.Controls.Add(_checkBox3);

        // Size Selection group (radio buttons + nested Priority group)
        var sizeGroup = new GroupBox
        {
            Text = "Size Selection",
            Location = new Point(220, 120),
            Size = new Size(200, 200),
            Name = "SizeSelectionGroup",
        };
        formControlsTab.Controls.Add(sizeGroup);

        _radioSmall = new RadioButton
        {
            Text = "Small",
            Location = new Point(10, 25),
            Size = new Size(80, 25),
            Name = "SmallRadio",
        };
        _radioSmall.CheckedChanged += (_, _) =>
        {
            if (_radioSmall.Checked)
            {
                UpdateStatus("Size: Small");
            }
        };
        sizeGroup.Controls.Add(_radioSmall);

        _radioMedium = new RadioButton
        {
            Text = "Medium",
            Location = new Point(10, 55),
            Size = new Size(80, 25),
            Name = "MediumRadio",
            Checked = true,
        };
        _radioMedium.CheckedChanged += (_, _) =>
        {
            if (_radioMedium.Checked)
            {
                UpdateStatus("Size: Medium");
            }
        };
        sizeGroup.Controls.Add(_radioMedium);

        _radioLarge = new RadioButton
        {
            Text = "Large",
            Location = new Point(10, 85),
            Size = new Size(80, 25),
            Name = "LargeRadio",
        };
        _radioLarge.CheckedChanged += (_, _) =>
        {
            if (_radioLarge.Checked)
            {
                UpdateStatus("Size: Large");
            }
        };
        sizeGroup.Controls.Add(_radioLarge);

        // Nested Priority group
        var priorityGroup = new GroupBox
        {
            Text = "Priority",
            Location = new Point(10, 115),
            Size = new Size(180, 75),
            Name = "PriorityGroup",
        };
        sizeGroup.Controls.Add(priorityGroup);

        var priorityCombo = new ComboBox
        {
            Location = new Point(10, 25),
            Size = new Size(160, 23),
            Name = "PriorityCombo",
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        priorityCombo.Items.AddRange(["Low", "Medium", "High", "Critical"]);
        priorityCombo.SelectedIndex = 1;
        priorityGroup.Controls.Add(priorityCombo);

        // Combo box group
        var comboGroup = new GroupBox
        {
            Text = "Category",
            Location = new Point(430, 120),
            Size = new Size(200, 80),
            Name = "CategoryGroup",
        };
        formControlsTab.Controls.Add(comboGroup);

        _comboBox = new ComboBox
        {
            Location = new Point(10, 30),
            Size = new Size(180, 23),
            Name = "CategoryCombo",
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        _comboBox.Items.AddRange(["Technology", "Science", "Art", "Music", "Sports"]);
        _comboBox.SelectedIndex = 0;
        _comboBox.SelectedIndexChanged += (_, _) => UpdateStatus($"Category: {_comboBox.SelectedItem}");
        comboGroup.Controls.Add(_comboBox);

        // Slider and Progress group
        var progressGroup = new GroupBox
        {
            Text = "Progress & Volume",
            Location = new Point(10, 250),
            Size = new Size(620, 120),
            Name = "ProgressGroup",
        };
        formControlsTab.Controls.Add(progressGroup);

        var sliderLabel = new Label { Text = "Volume:", Location = new Point(10, 30), Size = new Size(60, 20) };
        progressGroup.Controls.Add(sliderLabel);
        _slider = new TrackBar
        {
            Location = new Point(70, 20),
            Size = new Size(250, 45),
            Name = "VolumeSlider",
            Minimum = 0,
            Maximum = 100,
            Value = 50,
            TickFrequency = 10,
        };
        _slider.ValueChanged += (_, _) => UpdateStatus($"Volume: {_slider.Value}%");
        progressGroup.Controls.Add(_slider);

        var progressLabel = new Label { Text = "Progress:", Location = new Point(340, 30), Size = new Size(60, 20) };
        progressGroup.Controls.Add(progressLabel);
        _progressBar = new ProgressBar
        {
            Location = new Point(410, 25),
            Size = new Size(200, 25),
            Name = "MainProgressBar",
            Minimum = 0,
            Maximum = 100,
            Value = 65,
        };
        progressGroup.Controls.Add(_progressBar);

        var progressValueLabel = new Label
        {
            Text = "65%",
            Location = new Point(530, 55),
            Size = new Size(40, 20),
            Name = "ProgressValueLabel",
        };
        progressGroup.Controls.Add(progressValueLabel);

        // Progress buttons
        var decreaseBtn = new Button { Text = "-10", Location = new Point(410, 60), Size = new Size(50, 25), Name = "DecreaseProgressBtn" };
        decreaseBtn.Click += (_, _) =>
        {
            if (_progressBar.Value >= 10)
            {
                _progressBar.Value -= 10;
            }

            progressValueLabel.Text = $"{_progressBar.Value}%";
        };
        progressGroup.Controls.Add(decreaseBtn);

        var increaseBtn = new Button { Text = "+10", Location = new Point(470, 60), Size = new Size(50, 25), Name = "IncreaseProgressBtn" };
        increaseBtn.Click += (_, _) =>
        {
            if (_progressBar.Value <= 90)
            {
                _progressBar.Value += 10;
            }

            progressValueLabel.Text = $"{_progressBar.Value}%";
        };
        progressGroup.Controls.Add(increaseBtn);

        // =====================
        // List View Tab
        // =====================
        _listView = new ListView
        {
            Location = new Point(10, 10),
            Size = new Size(620, 400),
            Name = "ItemsListView",
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
        };
        _listView.Columns.AddRange([
            new ColumnHeader { Text = "ID", Width = 60 },
            new ColumnHeader { Text = "Name", Width = 200 },
            new ColumnHeader { Text = "Status", Width = 100 },
            new ColumnHeader { Text = "Date", Width = 120 },
        ]);
        _listView.Items.AddRange([
            new ListViewItem(["1", "Project Alpha", "Active", "2024-01-15"]) { Name = "Item1" },
            new ListViewItem(["2", "Project Beta", "Pending", "2024-02-20"]) { Name = "Item2" },
            new ListViewItem(["3", "Project Gamma", "Completed", "2024-03-10"]) { Name = "Item3" },
            new ListViewItem(["4", "Project Delta", "Active", "2024-04-05"]) { Name = "Item4" },
            new ListViewItem(["5", "Project Epsilon", "On Hold", "2024-05-15"]) { Name = "Item5" },
        ]);
        _listView.SelectedIndexChanged += (_, _) =>
        {
            if (_listView.SelectedItems.Count > 0)
            {
                UpdateStatus($"Selected: {_listView.SelectedItems[0].SubItems[1].Text}");
            }
        };
        listViewTab.Controls.Add(_listView);

        // =====================
        // Tree View Tab
        // =====================
        _treeView = new TreeView
        {
            Location = new Point(10, 10),
            Size = new Size(620, 400),
            Name = "FolderTreeView",
        };

        var root = new TreeNode("Documents") { Name = "DocumentsNode" };
        var work = new TreeNode("Work") { Name = "WorkNode" };
        work.Nodes.AddRange([
            new TreeNode("Reports") { Name = "ReportsNode" },
            new TreeNode("Presentations") { Name = "PresentationsNode" },
            new TreeNode("Spreadsheets") { Name = "SpreadsheetsNode" },
        ]);
        var personal = new TreeNode("Personal") { Name = "PersonalNode" };
        personal.Nodes.AddRange([
            new TreeNode("Photos") { Name = "PhotosNode" },
            new TreeNode("Music") { Name = "MusicNode" },
            new TreeNode("Videos") { Name = "VideosNode" },
        ]);
        root.Nodes.Add(work);
        root.Nodes.Add(personal);
        root.Nodes.Add(new TreeNode("Downloads") { Name = "DownloadsNode" });
        _treeView.Nodes.Add(root);
        root.Expand();

        _treeView.AfterSelect += (_, e) =>
        {
            if (e.Node != null)
            {
                UpdateStatus($"Selected folder: {e.Node.Text}");
            }
        };
        treeViewTab.Controls.Add(_treeView);

        // =====================
        // Data Grid Tab
        // =====================
        _dataGrid = new DataGridView
        {
            Location = new Point(10, 10),
            Size = new Size(620, 400),
            Name = "ProductsDataGrid",
            AllowUserToAddRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        };
        _dataGrid.Columns.AddRange([
            new DataGridViewTextBoxColumn { Name = "ProductId", HeaderText = "ID" },
            new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "Product Name" },
            new DataGridViewTextBoxColumn { Name = "Price", HeaderText = "Price" },
            new DataGridViewTextBoxColumn { Name = "Stock", HeaderText = "Stock" },
            new DataGridViewCheckBoxColumn { Name = "Available", HeaderText = "Available" },
        ]);
        _dataGrid.Rows.Add("P001", "Laptop Pro 15", "$1,299.00", "45", true);
        _dataGrid.Rows.Add("P002", "Wireless Mouse", "$29.99", "150", true);
        _dataGrid.Rows.Add("P003", "USB-C Hub", "$49.99", "0", false);
        _dataGrid.Rows.Add("P004", "Mechanical Keyboard", "$129.99", "30", true);
        _dataGrid.Rows.Add("P005", "4K Monitor", "$399.99", "12", true);

        _dataGrid.SelectionChanged += (_, _) =>
        {
            if (_dataGrid.SelectedRows.Count > 0)
            {
                var row = _dataGrid.SelectedRows[0];
                UpdateStatus($"Selected product: {row.Cells["ProductName"].Value}");
            }
        };
        dataGridTab.Controls.Add(_dataGrid);
    }

    /// <summary>
    /// Positions the form on the specified monitor.
    /// </summary>
    public void PositionOnMonitor(Screen screen)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var x = screen.Bounds.X + (screen.Bounds.Width - Width) / 2;
        var y = screen.Bounds.Y + (screen.Bounds.Height - Height) / 2;
        Location = new Point(x, y);
    }

    /// <summary>
    /// Resets all state to initial values.
    /// </summary>
    public void Reset()
    {
        SubmitClickCount = 0;
        CancelClickCount = 0;
        _usernameInput.Clear();
        _passwordInput.Clear();
        _checkBox1.Checked = true;
        _checkBox2.Checked = false;
        _checkBox3.Checked = false;
        _radioMedium.Checked = true;
        _comboBox.SelectedIndex = 0;
        _slider.Value = 50;
        _progressBar.Value = 65;
        _listView.SelectedItems.Clear();
        _treeView.SelectedNode = null;
        _dataGrid.ClearSelection();
        _tabControl.SelectedIndex = 0;
        UpdateStatus("UI Test Harness Reset");
    }

    private void UpdateStatus(string message)
    {
        _statusLabel.Text = message;
    }
}
