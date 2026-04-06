fn main() -> i32 {
    let b: Box(i32) = Box.New(42);
    let r: &uniq i32 = &uniq *b;
    let s: &i32 = &*b;
    *r + *s
}
