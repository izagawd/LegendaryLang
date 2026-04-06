fn main() -> i32 {
    let a: Box(i32) = Box.New(10);
    let b: Box(i32) = Box.New(32);
    *a + *b
}
