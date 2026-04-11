fn main() -> i32 {
    let b: GcMut(i32) = GcMut.New(42);
    let r1: &mut i32 = &mut *b;
    let r2: &mut i32 = &mut *b;
    *r1 + *r2
}
