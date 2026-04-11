fn main() -> i32 {
    let b: Box(i32) = Box.New(42);
    let r1: &i32 = &*b;
    let r2: &mut i32 = &mut *b;
    *r1 + *r2
}
