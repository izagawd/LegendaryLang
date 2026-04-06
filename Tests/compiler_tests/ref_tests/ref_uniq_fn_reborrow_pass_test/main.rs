fn yo(dd: &uniq i32) {
    *dd = *dd + 5;
}

fn main() -> i32 {
    let bro = 5;
    let unique = &uniq bro;
    yo(unique);
    *unique
}
