trait MetaSized {
    let Metadata :! Copy;
}

trait Sized: MetaSized {}

trait Copy: Sized {}
impl Copy for i32 {}
impl Copy for bool {}
impl Copy for u8 {}
impl Copy for usize {}
impl Copy for () {}
impl[T:! MetaSized] Copy for &T {}
impl[T:! MetaSized] Copy for &const T {}
impl[T:! MetaSized] Copy for &mut T {}

impl[T:! MetaSized] Copy for *shared T {}
impl[T:! MetaSized] Copy for *const T {}
impl[T:! MetaSized] Copy for *mut T {}
impl[T:! MetaSized] Copy for *uniq T {}

trait MutReassign {}
impl MutReassign for i32 {}
impl MutReassign for bool {}
impl MutReassign for u8 {}
impl MutReassign for usize {}
impl MutReassign for () {}
impl[T:! MetaSized] MutReassign for &T {}
impl[T:! MetaSized] MutReassign for &const T {}
impl[T:! MetaSized] MutReassign for &mut T {}
impl[T:! MetaSized] MutReassign for &uniq T {}
impl[T:! MetaSized] MutReassign for *shared T {}
impl[T:! MetaSized] MutReassign for *const T {}
impl[T:! MetaSized] MutReassign for *mut T {}
impl[T:! MetaSized] MutReassign for *uniq T {}

trait Primitive {}
impl Primitive for i32 {}
impl Primitive for u8 {}
impl Primitive for usize {}
impl Primitive for bool {}
