fn takes_ref(r: &mut i32) -> i32 {
    *r
}
fn main() -> i32 {
    let a = 5;
    let r = &mut a;
    takes_ref(r);
    takes_ref(r)
}
