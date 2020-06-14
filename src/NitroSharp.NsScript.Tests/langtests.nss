function if_else_basic() {
    $cond = false;
    if ($cond) {
        $consequence = true;
    }
    else {
        $alternative = true;
    }
    assert_eq(true, $alternative);
    assert_eq(false, $consequence);

    $cond = true;
    $consequence = false;
    $alternative = false;
    if ($cond) {
        $consequence = true;
    }
    else {
        $alternative = true;
    }
    assert_eq(true, $consequence);
    assert_eq(false, $alternative);
}

function break_from_inner_while_loop() {
    while ($outerIter < 5) {
        while (true) {
            $innerIter++;
            break;
        }
        $outerIter++;
    }
    assert_eq(5, $outerIter);
    assert_eq($outerIter, $innerIter);
}

function break_from_nested_if() {
    while ($whileIter < 5) {
        if ($whileIter <= 3) {
            $ifCounter++;
            if ($whileIter == 3) {
                break;
            }
        }
        $whileIter++;
    }
    assert_eq(3, $whileIter);
    assert_eq(4, $ifCounter);
}

function o_front_bug() {
    $a = 44;
    $b = $a-2;
    assert_eq(42, $b);
    $o = 100;
    $front = 20;
    $o-front = 44;
    $b = $o-front;
    assert_eq(44, $b);
}

function parameters_become_globals() {
    priv_foo();
    assert_eq(42, $uniqueparam42);
}

function priv_foo() {
    priv_bar(42);
}

function priv_bar($uniqueparam42) {
}
