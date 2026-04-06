fn takes_ref(r: &i32) -> i32 {
    *r
}
fn main() -> i32 {
    let a = 5;
    let r = &a;
    takes_ref(r) + takes_ref(r)
}
