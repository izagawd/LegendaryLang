fn get_ref(r: &i32) -> &i32 {
    r
}
fn main() -> i32 {
    let a = 55;
    *get_ref(&a)
}
