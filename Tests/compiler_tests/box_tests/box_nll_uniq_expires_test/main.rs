fn main() -> i32 {
    let b: Box(i32) = Box.New(10);
    let r1: &uniq i32 = &uniq *b;
    *r1 = 42;
    let r2: &i32 = &*b;
    *r2
}
