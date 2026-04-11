fn main() -> i32 {
    let arr = [1, 2, 3, 4, 5];
    let sum = 0;
    match arr.get_ref(0) { Option.Some(v) => { sum = sum + *v; }, Option.None => {} };
    match arr.get_ref(1) { Option.Some(v) => { sum = sum + *v; }, Option.None => {} };
    match arr.get_ref(2) { Option.Some(v) => { sum = sum + *v; }, Option.None => {} };
    match arr.get_ref(3) { Option.Some(v) => { sum = sum + *v; }, Option.None => {} };
    match arr.get_ref(4) { Option.Some(v) => { sum = sum + *v; }, Option.None => {} };
    sum
}
