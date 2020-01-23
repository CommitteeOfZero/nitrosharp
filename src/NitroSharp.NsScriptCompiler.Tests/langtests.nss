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
        if ($whileIter < 3) {
            $ifCounter++;
            break;
            // unreachable
            $ifCounter = 0;
        }
        $whileIter++;
    }
    assert_eq(5, $whileIter);
    assert_eq(3, $ifCounter);
}
