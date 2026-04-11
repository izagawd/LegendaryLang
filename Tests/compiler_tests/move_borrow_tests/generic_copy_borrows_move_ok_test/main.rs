fn use_and_move[T:! Sized +Copy](val: T) -> T {
    let r: &T = &val;
    let _x = *r;
    val
}

fn main() -> i32 {
    use_and_move(42)
}
