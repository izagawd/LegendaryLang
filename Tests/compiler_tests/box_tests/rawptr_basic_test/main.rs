fn main() -> i32 {
    let b: Box(i32) = Box.New(42);
    let val: i32 = *b;
    val
}
