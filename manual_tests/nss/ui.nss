
function test_ui()
{
	test_ui_multiple_hit_areas();
    test_ui_windowed_choice();
    Exit();
}

// expect: only 'foo' is used for hit-tests
// FAIL
function test_ui_multiple_hit_areas()
{
	CreateName("test");
	CreateChoice("test/choice");
	CreateColor("test/choice/MouseUsual/foo", 1, 0, 0, 256, 256, "green");
	CreateColor("test/choice/MouseUsual/foo/bar", 2, 128, 128, 256, 256, "blue");
	CreateColor("test/choice/MouseOver/rect", 1, 0, 0, 256, 256, "red");
	Fade("test/choice/MouseOver/rect", 0, 0, null, true);
	select {
		case test/choice {}
	}
	Delete("test");
}

// expect: only the cropped area is used for hit-tests
// FAIL
function test_ui_windowed_choice()
{
	CreateName("test");
	CreateWindow("test/base", 1, 64, 64, 128, 128, false);
	CreateChoice("test/base/choice");
	CreateColor("test/base/choice/MouseUsual/rect", 1, 0, 0, 256, 256, "green");
	CreateColor("test/base/choice/MouseOver/rect", 1, 0, 0, 256, 256, "red");
	Fade("test/base/choice/MouseOver/rect", 0, 0, null, true);
	select {
		case test/base/choice {}
	}
	Delete("test");
}
