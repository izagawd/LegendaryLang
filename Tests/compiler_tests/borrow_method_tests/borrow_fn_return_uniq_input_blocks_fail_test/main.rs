fn bro(dd: &uniq i32) -> &i32 {
    &*dd
}
fn main() -> i32 {
    let made = 5;
    let gotten = bro(&uniq made);
    let another = &made;
    *gotten
}
