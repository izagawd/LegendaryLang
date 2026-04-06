fn main() -> i32 {
    let b: Box(i32) = Box.New(42);
    *b = 99;
    *b
}
