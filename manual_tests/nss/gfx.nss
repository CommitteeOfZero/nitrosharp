
chapter main
{
    child_rect();
    //move_aliased_child();
    Exit();
}

// expect: both objects fade away
// OK
function child_rect()
{
    CreateColor("rect", 1000, center, middle, 200, 200, "BLUE");
    WaitKey();
    CreateColor("rect/child", 1000, 0, 0, 50, 50, "RED");
    Fade("rect", 3000, 0, null, true);
    WaitKey();
}

// expect: 'a' gets Move'd twice
// OK
function move_aliased_child()
{
    CreateColor("a", 1, 0, 0, 256, 256, "blue");
    CreateColor("a/b", 2, 0, 0, 128, 128, "red");
    SetAlias("a", "a");
    SetAlias("a/b", "b");
    Move("@*", 0, @32, @32, null, true);
    WaitKey();
}