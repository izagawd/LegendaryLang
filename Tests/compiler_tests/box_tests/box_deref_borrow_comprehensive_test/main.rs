fn read_through(r: &Box(i32)) -> i32 {
    **r
}

fn write_through(r: &uniq Box(i32), val: i32) {
    **r = val;
}

fn get_inner_ref(r: &Box(i32)) -> &i32 {
    &**r
}

fn main() -> i32 {
    let b: Box(i32) = Box.New(0);

    write_through(&uniq b, 42);

    let val1: i32 = read_through(&b);

    let inner: &i32 = get_inner_ref(&b);
    let val2: i32 = *inner;

    val1 + val2 - 42
}
