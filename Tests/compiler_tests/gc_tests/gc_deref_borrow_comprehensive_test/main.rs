fn read_through(r: &GcMut(i32)) -> i32 {
    **r
}

fn write_through(r: &mut GcMut(i32), val: i32) {
    **r = val;
}

fn get_inner_ref(r: &GcMut(i32)) -> &i32 {
    &**r
}

fn main() -> i32 {
    let b: GcMut(i32) = GcMut.New(0);

    write_through(&mut b, 42);

    let val1: i32 = read_through(&b);

    let inner: &i32 = get_inner_ref(&b);
    let val2: i32 = *inner;

    val1 + val2 - 42
}
