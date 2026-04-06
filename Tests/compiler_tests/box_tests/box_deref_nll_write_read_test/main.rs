fn main() -> i32 {
    let b: Box(i32) = Box.New(0);
    let w: &uniq i32 = &uniq *b;
    *w = 42;
    let r: &i32 = &*b;
    *r
}
