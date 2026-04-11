fn main() -> i32 {
    let b: Box(i32) = Box.New(42);
    let r: &mut i32 = &mut *b;
    let s: &i32 = &*b;
    *r + *s
}
