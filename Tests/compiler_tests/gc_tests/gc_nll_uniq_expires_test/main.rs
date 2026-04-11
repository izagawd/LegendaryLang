fn main() -> i32 {
    let b: Gc(i32) = Gc.New(10);
    let r1: &mut i32 = &mut *b;
    *r1 = 42;
    let r2: &i32 = &*b;
    *r2
}
