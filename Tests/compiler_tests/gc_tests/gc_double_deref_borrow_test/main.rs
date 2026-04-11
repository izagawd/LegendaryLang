fn borrower(dd: &mut GcMut(i32)) -> &mut i32 {
    &mut **dd
}

fn main() -> i32 {
    let dd: GcMut(i32) = GcMut.New(4);
    *borrower(&mut dd)
}
