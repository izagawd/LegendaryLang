fn try_mutate(r: &mut &i32) {
    **r = 42;
}

fn main() -> i32 {
    let x = 0;
    let s = &x;
    try_mutate(&mut s);
    x
}
