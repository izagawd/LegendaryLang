fn main() -> i32 {
    let b: Gc(i32) = Gc.New(42);
    let r1: &mut i32 = &mut *b;
    let r2: &i32 = &*b;
    *r1 + *r2
}
