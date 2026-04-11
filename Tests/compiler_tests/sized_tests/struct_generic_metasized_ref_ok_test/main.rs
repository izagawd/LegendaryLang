struct RefHolder['a](T:! type) { ptr: &'a T }
fn get_val(r: RefHolder(i32)) -> i32 { *r.ptr }
fn main() -> i32 {
    let x: i32 = 99;
    let r = make RefHolder { ptr: &x };
    get_val(r)
}
