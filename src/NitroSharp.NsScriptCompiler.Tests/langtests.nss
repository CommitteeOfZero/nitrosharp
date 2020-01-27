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
