fn main() -> i32 {
    let b: Box(i32) = Box.New(42);
    let r1: &uniq i32 = &uniq *b;
    let r2: &uniq i32 = &uniq *b;
    *r1 + *r2
}
