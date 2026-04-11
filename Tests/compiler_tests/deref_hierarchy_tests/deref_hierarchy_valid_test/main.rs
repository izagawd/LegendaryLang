fn from_shared(r: &i32) -> i32 {
    let a: &i32 = &*r;
    *a
}


fn from_mut_to_shared(r: &mut i32) -> i32 {
    let a: &i32 = &*r;
    *a
}


fn main() -> i32 {
    let x: i32 = 6;
    from_shared(&x)
    + from_mut_to_shared(&mut x)
}
