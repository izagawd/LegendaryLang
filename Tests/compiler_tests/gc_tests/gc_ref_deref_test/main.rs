fn read_ref(r: &i32) -> i32 {
    *r
}

fn main() -> i32 {
    let b: GcMut(i32) = GcMut.New(42);
    let r: &i32 = &*b;
    read_ref(r)
}
