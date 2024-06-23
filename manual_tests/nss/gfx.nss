
function test_gfx()
{
    test_gfx_letterboxing_clip();
    test_gfx_letterboxing_fit();
    test_gfx_child_rect();
    test_gfx_move_aliased_child();
}

// expect: red rectangle is not visible at any resolutions
// FAIL
function test_gfx_letterboxing_clip()
{
	CreateName("test");
    CreateColor("test/bg", 500, center, middle, 2560, 1440, "RED");
    CreateColor("test/fg", 1000, center, middle, 1280, 720, "BLUE");
    WaitKey();
    Delete("test");
}

// expect: blue rectangle is centered and scaled to fit the window
// OK
function test_gfx_letterboxing_fit()
{
	CreateName("test");
    CreateColor("test/fg", 1000, center, middle, 1280, 720, "BLUE");
    WaitKey();
    Delete("test");
}

// expect: both objects fade away
// OK
function test_gfx_child_rect()
{
	CreateName("test");
    CreateColor("test/rect", 1000, center, middle, 200, 200, "BLUE");
    WaitKey();
    CreateColor("test/rect/child", 1000, 0, 0, 50, 50, "RED");
    Fade("test/rect", 3000, 0, null, true);
    WaitKey();
    Delete("test");
}

// expect: 'a' gets Move'd twice
// OK
function test_gfx_move_aliased_child()
{
	CreateName("test");
    CreateColor("test/a", 1, 0, 0, 256, 256, "blue");
    CreateColor("test/a/b", 2, 0, 0, 128, 128, "red");
    SetAlias("test/a", "a");
    SetAlias("test/a/b", "b");
    Move("@*", 0, @32, @32, null, true);
    WaitKey();
    Delete("test");
}
