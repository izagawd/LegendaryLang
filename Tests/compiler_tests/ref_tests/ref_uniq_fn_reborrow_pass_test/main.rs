fn yo(dd: &mut i32) {
    *dd = *dd + 5;
}

fn main() -> i32 {
    let bro = 5;
    let unique = &mut bro;
    yo(unique);
    *unique
}
