fn mutate_through(r: &&mut i32) {
    **r = 42;
}

fn main() -> i32 {
    let x = 0;
    let m = &mut x;
    mutate_through(&m);
    x
}
