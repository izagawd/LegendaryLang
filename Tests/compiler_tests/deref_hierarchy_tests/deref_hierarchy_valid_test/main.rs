fn from_shared(r: &i32) -> i32 {
    let a: &i32 = &*r;
    *a
}

fn from_const_to_shared(r: &const i32) -> i32 {
    let a: &i32 = &*r;
    *a
}

fn from_mut_to_shared(r: &mut i32) -> i32 {
    let a: &i32 = &*r;
    *a
}

fn from_uniq_to_shared(r: &uniq i32) -> i32 {
    let a: &i32 = &*r;
    *a
}

fn from_uniq_to_const(r: &uniq i32) -> i32 {
    let a: &const i32 = &const *r;
    *a
}

fn from_uniq_to_mut(r: &uniq i32) -> i32 {
    let a: &mut i32 = &mut *r;
    *a
}

fn from_uniq_to_uniq(r: &uniq i32) -> i32 {
    let a: &uniq i32 = &uniq *r;
    *a
}

fn main() -> i32 {
    let x: i32 = 6;
    from_shared(&x)
    + from_const_to_shared(&const x)
    + from_mut_to_shared(&mut x)
    + from_uniq_to_shared(&uniq x)
    + from_uniq_to_const(&uniq x)
    + from_uniq_to_mut(&uniq x)
    + from_uniq_to_uniq(&uniq x)
}
