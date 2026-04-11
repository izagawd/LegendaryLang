fn main() -> i32 {
    let b: Box(i32) = Box.New(42);
    let r1: &mut i32 = &mut *b;
    let r2: &i32 = &*b;
    *r1 + *r2
}
